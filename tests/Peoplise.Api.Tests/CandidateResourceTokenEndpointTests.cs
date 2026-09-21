using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Peoplise.Infrastructure.Security;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;

namespace Peoplise.Api.Tests;

/// <summary>
/// Proves the ADR 0004 candidate-token guard actually runs against the real HTTP
/// pipeline, not just the token service in isolation (see
/// Peoplise.Infrastructure.Tests.Security.HmacCandidateResourceTokenServiceTests for
/// that). Seeds a Conversation directly via the repository — the response endpoint's
/// own business logic isn't what's under test here, only whether the request even
/// reaches it.
/// </summary>
public class CandidateResourceTokenEndpointTests
{
    private static async Task<Guid> SeedConversationAsync(ApiTestFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var conversations = scope.ServiceProvider.GetRequiredService<IRepository<Conversation, ConversationId>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var conversation = Conversation.Start(
            BotProjectId.New(), Guid.NewGuid(), ConversationInterface.WebChat,
            Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        await conversations.AddAsync(conversation);
        await unitOfWork.SaveChangesAsync();

        return conversation.Id.Value;
    }

    [Fact]
    public async Task Responding_with_no_token_is_rejected()
    {
        await using var factory = new ApiTestFactory();
        var conversationId = await SeedConversationAsync(factory);
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync($"/api/conversations/{conversationId}/responses", new { response = "hi" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Responding_with_a_token_for_a_different_conversation_is_rejected()
    {
        await using var factory = new ApiTestFactory();
        var conversationId = await SeedConversationAsync(factory);
        var tokenService = factory.Services.GetRequiredService<ICandidateResourceTokenService>();
        var tokenForSomeoneElsesConversation = tokenService.Issue(
            CandidateResourceType.Conversation, Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1));

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Candidate-Token", tokenForSomeoneElsesConversation);

        var response = await client.PostAsJsonAsync($"/api/conversations/{conversationId}/responses", new { response = "hi" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Responding_with_the_matching_token_reaches_the_real_handler()
    {
        await using var factory = new ApiTestFactory();
        var conversationId = await SeedConversationAsync(factory);
        var tokenService = factory.Services.GetRequiredService<ICandidateResourceTokenService>();
        var token = tokenService.Issue(CandidateResourceType.Conversation, conversationId, DateTimeOffset.UtcNow.AddDays(1));

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Candidate-Token", token);

        var response = await client.PostAsJsonAsync($"/api/conversations/{conversationId}/responses", new { response = "hi" });

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized, "a matching token must let the request through the guard, whatever the handler then does with it");
    }

    [Fact]
    public async Task Starting_a_conversation_never_needs_a_token_itself()
    {
        await using var factory = new ApiTestFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/conversations", new { positionId = Guid.NewGuid(), candidateId = Guid.NewGuid(), @interface = "WebChat" });

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized, "Start is where a token is issued, not where one is required");
    }
}
