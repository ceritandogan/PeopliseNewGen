using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Peoplise.Infrastructure.Modules;
using Peoplise.Infrastructure.Persistence;
using Peoplise.Modules.VideoInterview.Application.Cases.Commands;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.MultiTenancy;
using Peoplise.SharedKernel.Persistence;
using Xunit;

namespace Peoplise.Modules.VideoInterview.Tests.Application;

public class StartCandidateCaseCommandHandlerTests
{
    private static (
        AppDbContext Context,
        Guid TenantId,
        IRepository<Case, CaseId> Cases,
        IUnitOfWork UnitOfWork,
        StartCandidateCaseCommandHandler Handler) CreateHandler()
    {
        var tenantId = Guid.NewGuid();
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(TenantId.From(tenantId));

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options, tenantContext, new ModuleAssemblyRegistry([typeof(CaseBotProject).Assembly]));

        var cases = Substitute.For<IRepository<Case, CaseId>>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new StartCandidateCaseCommandHandler(context, cases, unitOfWork);
        return (context, tenantId, cases, unitOfWork, handler);
    }

    [Fact]
    public async Task Returns_NotFound_when_no_case_bot_project_is_configured_for_the_position()
    {
        var (_, _, _, _, handler) = CreateHandler();

        var result = await handler.Handle(new StartCandidateCaseCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CaseBotProject.NotFound");
    }

    [Fact]
    public async Task Starts_the_case_on_the_default_flows_first_step_and_returns_its_content()
    {
        var (context, tenantId, cases, unitOfWork, handler) = CreateHandler();
        var positionId = Guid.NewGuid();
        var project = CaseBotProject.Create("Backend Case Study", positionId, retakesAllowed: 1, retentionPeriodDays: 90).Value;
        project.TenantId = tenantId;
        var flow = project.AddFlow("Main Flow", isDefault: true).Value;
        var firstStep = flow.AddStep(
            StepType.RecordVideoAnswer, "Tell us about a challenge you solved.",
            order: 0, preparationTimeSeconds: 10, recordingTimeSeconds: 90).Value;
        context.Set<CaseBotProject>().Add(project);
        await context.SaveChangesAsync();

        var result = await handler.Handle(new StartCandidateCaseCommand(positionId, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.StepId.Should().Be(firstStep.Id);
        result.Value.RetakesAllowed.Should().Be(1);
        result.Value.PreparationTimeSeconds.Should().Be(10);
        result.Value.RecordingTimeSeconds.Should().Be(90);
        await cases.Received(1).AddAsync(
            Arg.Is<Case>(c => c.CurrentFlowId == flow.Id && c.CurrentStepId == firstStep.Id),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
