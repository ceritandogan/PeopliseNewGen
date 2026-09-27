using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.Entities;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.MultiTenancy;
using Peoplise.SharedKernel.Persistence;
using Xunit;

namespace Peoplise.Api.Tests;

/// <summary>
/// Exercises CandidatesController.AddManual through the real HTTP pipeline (real DI,
/// real MediatR handlers) — same style as ProtectedEndpointTests/
/// EvaluationTriggersStageTransitionTests. Doesn't re-prove StartConversationCommand's or
/// StartCandidateCaseCommand's own domain logic (covered in their own module test
/// projects) — only the new orchestration this endpoint adds: creating the
/// CandidateProcess, best-effort starting whichever project type(s) exist, and the
/// email-based duplicate check specific to manual add (see
/// CandidateExistsForPositionQuery's remarks on why SubmitCandidateApplicationCommand's
/// own CandidateId-based check can't catch this case).
/// </summary>
public class AddCandidateManuallyEndpointTests
{
    private static async Task<(PositionId PositionId, WorkflowDefinition Workflow)> SeedPositionWithWorkflowAsync(
        IServiceProvider services, TenantId tenantId)
    {
        var positions = services.GetRequiredService<IRepository<Position, PositionId>>();
        var workflows = services.GetRequiredService<IRepository<WorkflowDefinition, WorkflowDefinitionId>>();
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();

        var position = Position.Create("Backend Engineer", "Engineering", "Istanbul", "Turkey", WorkMode.Remote, SeniorityLevel.Mid, EmploymentType.FullTime);
        position.TenantId = tenantId.Value;

        var workflow = WorkflowDefinition.Create("Standard Flow");
        workflow.TenantId = tenantId.Value;
        workflow.AddStage("Application Review", StageType.ScreeningTest, order: 0);
        position.AssignWorkflow(workflow.Id);

        await workflows.AddAsync(workflow);
        await positions.AddAsync(position);
        await unitOfWork.SaveChangesAsync();

        return (position.Id, workflow);
    }

    private static HttpClient AuthenticatedClient(ApiTestFactory factory, TenantId tenantId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add(TestAuthHandler.TenantIdHeader, tenantId.Value.ToString());
        return client;
    }

    [Fact]
    public async Task Adding_a_candidate_to_a_position_with_no_project_configured_succeeds_with_no_links()
    {
        var tenantId = TenantId.New();
        using var tenantScope = AmbientTenantOverride.Begin(tenantId);

        await using var factory = new ApiTestFactory();
        using var scope = factory.Services.CreateScope();
        var (positionId, _) = await SeedPositionWithWorkflowAsync(scope.ServiceProvider, tenantId);

        var client = AuthenticatedClient(factory, tenantId);
        var response = await client.PostAsJsonAsync("/api/candidates/manual", new
        {
            positionId = positionId.Value,
            candidateName = "Ada Lovelace",
            candidateEmail = "ada@example.com",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AddManualResponseDto>();
        body!.CandidateProcessId.Should().NotBeEmpty();
        body.Links.Should().BeEmpty("neither a BotProject nor a CaseBotProject exists for this position yet");
    }

    [Fact]
    public async Task Adding_a_candidate_to_a_position_with_a_bot_project_returns_a_working_bot_chat_link()
    {
        var tenantId = TenantId.New();
        using var tenantScope = AmbientTenantOverride.Begin(tenantId);

        await using var factory = new ApiTestFactory();
        using var scope = factory.Services.CreateScope();
        var (positionId, _) = await SeedPositionWithWorkflowAsync(scope.ServiceProvider, tenantId);

        var botProjects = scope.ServiceProvider.GetRequiredService<IRepository<BotProject, BotProjectId>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var botProject = BotProject.Create("Screening Bot", positionId.Value, retentionPeriodDays: 180).Value;
        botProject.TenantId = tenantId.Value;
        var flow = botProject.AddFlow("Pre-screening", isDefault: true).Value;
        flow.AddStep(StepType.SendMessage, "Merhaba!", order: 0);
        await botProjects.AddAsync(botProject);
        await unitOfWork.SaveChangesAsync();

        var client = AuthenticatedClient(factory, tenantId);
        var response = await client.PostAsJsonAsync("/api/candidates/manual", new
        {
            positionId = positionId.Value,
            candidateName = "Grace Hopper",
            candidateEmail = "grace@example.com",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AddManualResponseDto>();
        body!.Links.Should().ContainSingle(link => link.Type == "bot-chat" && link.Url.Contains("token="));
    }

    [Fact]
    public async Task Adding_the_same_email_to_the_same_position_twice_is_rejected_as_a_duplicate()
    {
        var tenantId = TenantId.New();
        using var tenantScope = AmbientTenantOverride.Begin(tenantId);

        await using var factory = new ApiTestFactory();
        using var scope = factory.Services.CreateScope();
        var (positionId, _) = await SeedPositionWithWorkflowAsync(scope.ServiceProvider, tenantId);

        var client = AuthenticatedClient(factory, tenantId);
        var request = new
        {
            positionId = positionId.Value,
            candidateName = "Ada Lovelace",
            candidateEmail = "ada@example.com",
        };

        var first = await client.PostAsJsonAsync("/api/candidates/manual", request);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        // Each call mints its own server-side CandidateId (see AddManual's remarks), so
        // this proves the duplicate check is genuinely keyed by email, not by an id the
        // two requests happen to share.
        var second = await client.PostAsJsonAsync("/api/candidates/manual", request);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await second.Content.ReadAsStringAsync();
        body.Should().Contain("CandidateProcess.AlreadyApplied");
    }

    private sealed record AddManualResponseDto(Guid CandidateProcessId, List<AddManualLinkDto> Links);

    private sealed record AddManualLinkDto(string Type, string Url);
}
