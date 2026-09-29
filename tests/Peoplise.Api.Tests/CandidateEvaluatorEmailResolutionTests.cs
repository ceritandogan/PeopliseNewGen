using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Peoplise.Infrastructure.Identity;
using Peoplise.Infrastructure.Persistence;
using Peoplise.Modules.ATS.Application.Candidates.Commands;
using Peoplise.Modules.ATS.Application.Candidates.Queries;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.Entities;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.MultiTenancy;
using Peoplise.SharedKernel.Persistence;
using Xunit;

namespace Peoplise.Api.Tests;

/// <summary>
/// Proves GetCandidateDetailQuery resolves a real reviewer's email against
/// Peoplise.Infrastructure.Identity.User (a cross-cutting, non-module entity, so this
/// needs the real composed DI container — a mocked-repository unit test in
/// Peoplise.Modules.ATS.Tests can't reach it) rather than showing the raw JWT `sub` id
/// the panel's Timeline card used to display.
/// </summary>
public class CandidateEvaluatorEmailResolutionTests
{
    private static async Task<CandidateProcess> SeedCandidateAsync(IServiceProvider services, TenantId tenantId)
    {
        var workflows = services.GetRequiredService<IRepository<WorkflowDefinition, WorkflowDefinitionId>>();
        var processes = services.GetRequiredService<IRepository<CandidateProcess, CandidateProcessId>>();
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();

        var workflow = WorkflowDefinition.Create("Standard Flow");
        workflow.TenantId = tenantId.Value;
        var stage = workflow.AddStage("Application Review", StageType.ScreeningTest, order: 0).Value;
        await workflows.AddAsync(workflow);

        var process = CandidateProcess.Submit(
            CandidateId.New(), PositionId.New(), workflow.Id,
            "Ada Lovelace", "ada@example.com", null, null, stage.Id, DateTimeOffset.UtcNow);
        process.TenantId = tenantId.Value;
        await processes.AddAsync(process);
        await unitOfWork.SaveChangesAsync();

        return process;
    }

    [Fact]
    public async Task Resolves_the_evaluators_email_instead_of_their_raw_id()
    {
        var tenantId = TenantId.New();
        using var tenantScope = AmbientTenantOverride.Begin(tenantId);

        await using var factory = new ApiTestFactory();
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var process = await SeedCandidateAsync(services, tenantId);
        var mediator = services.GetRequiredService<IMediator>();
        var context = services.GetRequiredService<AppDbContext>();

        var reviewerId = Guid.NewGuid();
        context.Set<User>().Add(new User(reviewerId, tenantId.Value, "reviewer@example.com", passwordHash: "x", displayName: "Reviewer One", roles: []));
        await context.SaveChangesAsync();

        var evalResult = await mediator.Send(new SubmitEvaluationCommand(process.Id.Value, reviewerId.ToString(), 85, "Strong answer."));
        evalResult.IsSuccess.Should().BeTrue();

        var detailResult = await mediator.Send(new GetCandidateDetailQuery(process.Id.Value));

        detailResult.IsSuccess.Should().BeTrue();
        var evaluation = detailResult.Value.Evaluations.Single();
        evaluation.EvaluatorId.Should().Be(reviewerId.ToString(), "the stable id must still be there, just not what's shown");
        evaluation.EvaluatorEmail.Should().Be("reviewer@example.com");
    }

    [Fact]
    public async Task Falls_back_to_the_raw_evaluator_id_when_no_matching_user_exists()
    {
        var tenantId = TenantId.New();
        using var tenantScope = AmbientTenantOverride.Begin(tenantId);

        await using var factory = new ApiTestFactory();
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var process = await SeedCandidateAsync(services, tenantId);
        var mediator = services.GetRequiredService<IMediator>();

        var missingReviewerId = Guid.NewGuid().ToString();
        var evalResult = await mediator.Send(new SubmitEvaluationCommand(process.Id.Value, missingReviewerId, 60, null));
        evalResult.IsSuccess.Should().BeTrue();

        var detailResult = await mediator.Send(new GetCandidateDetailQuery(process.Id.Value));

        detailResult.IsSuccess.Should().BeTrue();
        detailResult.Value.Evaluations.Single().EvaluatorEmail.Should().Be(missingReviewerId);
    }
}
