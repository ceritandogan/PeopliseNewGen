using FluentAssertions;
using NSubstitute;
using Peoplise.Modules.VideoInterview.Application.CaseBotProjects.Commands;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Xunit;

namespace Peoplise.Modules.VideoInterview.Tests.Application;

public class AddFlowStepCommandHandlerTests
{
    private static CaseBotProject CreateProjectWithFlow(out Guid flowId)
    {
        var project = CaseBotProject.Create("Backend Case Study", Guid.NewGuid(), retakesAllowed: 1, retentionPeriodDays: 90).Value;
        var flow = project.AddFlow("Main Flow", isDefault: true).Value;
        flowId = flow.Id;
        return project;
    }

    [Fact]
    public async Task Returns_NotFound_when_the_project_does_not_exist()
    {
        var projects = Substitute.For<IRepository<CaseBotProject, CaseBotProjectId>>();
        projects.GetByIdAsync(Arg.Any<CaseBotProjectId>(), Arg.Any<CancellationToken>()).Returns((CaseBotProject?)null);
        var handler = new AddFlowStepCommandHandler(projects, Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(
            new AddFlowStepCommand(Guid.NewGuid(), Guid.NewGuid(), StepType.RecordVideoAnswer, "Tell us about a challenge.", 0, 10, 90, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CaseBotProject.NotFound");
    }

    [Fact]
    public async Task Returns_FlowNotFound_when_the_flow_is_not_part_of_the_project()
    {
        var project = CreateProjectWithFlow(out _);
        var projects = Substitute.For<IRepository<CaseBotProject, CaseBotProjectId>>();
        projects.GetByIdAsync(Arg.Any<CaseBotProjectId>(), Arg.Any<CancellationToken>()).Returns(project);
        var handler = new AddFlowStepCommandHandler(projects, Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(
            new AddFlowStepCommand(project.Id.Value, Guid.NewGuid(), StepType.RecordVideoAnswer, "Tell us about a challenge.", 0, 10, 90, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CaseBotProject.FlowNotFound");
    }

    [Fact]
    public async Task Adds_a_step_and_saves()
    {
        var project = CreateProjectWithFlow(out var flowId);
        var projects = Substitute.For<IRepository<CaseBotProject, CaseBotProjectId>>();
        projects.GetByIdAsync(Arg.Any<CaseBotProjectId>(), Arg.Any<CancellationToken>()).Returns(project);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new AddFlowStepCommandHandler(projects, unitOfWork);
        var competencyId = Guid.NewGuid();

        var result = await handler.Handle(
            new AddFlowStepCommand(
                project.Id.Value, flowId, StepType.RecordVideoAnswer, "Tell us about a challenge.",
                0, 10, 90, [competencyId]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var flow = project.FindFlow(flowId)!;
        flow.Steps.Should().ContainSingle(s =>
            s.Id == result.Value && s.Content == "Tell us about a challenge." && s.Order == 0
            && s.PreparationTimeSeconds == 10 && s.RecordingTimeSeconds == 90
            && s.RelatedCompetencyIds.Contains(competencyId));
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_a_duplicate_step_order_without_saving()
    {
        var project = CreateProjectWithFlow(out var flowId);
        project.FindFlow(flowId)!.AddStep(StepType.RecordVideoAnswer, "First question.", order: 0);
        var projects = Substitute.For<IRepository<CaseBotProject, CaseBotProjectId>>();
        projects.GetByIdAsync(Arg.Any<CaseBotProjectId>(), Arg.Any<CancellationToken>()).Returns(project);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new AddFlowStepCommandHandler(projects, unitOfWork);

        var result = await handler.Handle(
            new AddFlowStepCommand(project.Id.Value, flowId, StepType.RecordVideoAnswer, "Second question.", 0, null, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Flow.DuplicateStepOrder");
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
