using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Peoplise.Infrastructure.Modules;
using Peoplise.Infrastructure.Persistence;
using Peoplise.Modules.HrBot.Application.Conversations.Queries;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.MultiTenancy;
using Xunit;

namespace Peoplise.Modules.HrBot.Tests.Application;

public class GetConversationForCandidateQueryTests
{
    private static (AppDbContext Context, Guid TenantId, GetConversationForCandidateQueryHandler Handler) CreateHandler()
    {
        var tenantId = Guid.NewGuid();
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(TenantId.From(tenantId));

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options, tenantContext, new ModuleAssemblyRegistry([typeof(BotProject).Assembly]));

        return (context, tenantId, new GetConversationForCandidateQueryHandler(context));
    }

    [Fact]
    public async Task Returns_null_when_no_bot_project_exists_for_the_position()
    {
        var (_, _, handler) = CreateHandler();

        var result = await handler.Handle(new GetConversationForCandidateQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task Returns_null_when_the_candidate_has_not_started_a_conversation_for_this_position()
    {
        var (context, tenantId, handler) = CreateHandler();
        var positionId = Guid.NewGuid();
        var project = BotProject.Create("Screening Bot", positionId, retentionPeriodDays: 90).Value;
        project.TenantId = tenantId;
        context.Set<BotProject>().Add(project);
        await context.SaveChangesAsync();

        var result = await handler.Handle(new GetConversationForCandidateQuery(Guid.NewGuid(), positionId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task Returns_the_conversation_id_when_the_candidate_has_one_for_this_position()
    {
        var (context, tenantId, handler) = CreateHandler();
        var positionId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        var project = BotProject.Create("Screening Bot", positionId, retentionPeriodDays: 90).Value;
        project.TenantId = tenantId;
        context.Set<BotProject>().Add(project);
        var conversation = Conversation.Start(project.Id, candidateId, ConversationInterface.WebChat, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        conversation.TenantId = tenantId;
        context.Set<Conversation>().Add(conversation);
        await context.SaveChangesAsync();

        var result = await handler.Handle(new GetConversationForCandidateQuery(candidateId, positionId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(conversation.Id.Value);
    }

    [Fact]
    public async Task Does_not_match_a_conversation_belonging_to_a_different_candidate()
    {
        var (context, tenantId, handler) = CreateHandler();
        var positionId = Guid.NewGuid();
        var project = BotProject.Create("Screening Bot", positionId, retentionPeriodDays: 90).Value;
        project.TenantId = tenantId;
        context.Set<BotProject>().Add(project);
        var someoneElsesConversation = Conversation.Start(
            project.Id, Guid.NewGuid(), ConversationInterface.WebChat, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        someoneElsesConversation.TenantId = tenantId;
        context.Set<Conversation>().Add(someoneElsesConversation);
        await context.SaveChangesAsync();

        var result = await handler.Handle(new GetConversationForCandidateQuery(Guid.NewGuid(), positionId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task Returns_the_most_recently_started_conversation_when_more_than_one_exists()
    {
        var (context, tenantId, handler) = CreateHandler();
        var positionId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        var project = BotProject.Create("Screening Bot", positionId, retentionPeriodDays: 90).Value;
        project.TenantId = tenantId;
        context.Set<BotProject>().Add(project);

        var older = Conversation.Start(
            project.Id, candidateId, ConversationInterface.WebChat, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-2));
        older.TenantId = tenantId;
        var newer = Conversation.Start(
            project.Id, candidateId, ConversationInterface.WebChat, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        newer.TenantId = tenantId;
        context.Set<Conversation>().AddRange(older, newer);
        await context.SaveChangesAsync();

        var result = await handler.Handle(new GetConversationForCandidateQuery(candidateId, positionId), CancellationToken.None);

        result.Value.Should().Be(newer.Id.Value);
    }
}
