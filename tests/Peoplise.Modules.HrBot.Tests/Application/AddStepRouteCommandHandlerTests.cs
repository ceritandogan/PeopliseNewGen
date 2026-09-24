using FluentAssertions;
using FluentValidation;
using NSubstitute;
using Peoplise.Modules.HrBot.Application.BotProjects.Commands;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Xunit;

namespace Peoplise.Modules.HrBot.Tests.Application;

public class AddStepRouteCommandHandlerTests
{
    private static BotProject CreateProjectWithFlowAndStep(out Guid flowId, out Guid stepId)
    {
        var project = BotProject.Create("Backend Screening Bot", Guid.NewGuid(), retentionPeriodDays: 90).Value;
        var flow = project.AddFlow("Pre-screening", isDefault: true).Value;
        var step = flow.AddStep(StepType.SendQuickReply, "Ready to continue?", order: 0, quickReplyOptions: ["Yes", "No"]).Value;
        flowId = flow.Id;
        stepId = step.Id;
        return project;
    }

    private static (IRepository<BotProject, BotProjectId> Projects, IUnitOfWork UnitOfWork, AddStepRouteCommandHandler Handler) CreateHandler(BotProject? project)
    {
        var projects = Substitute.For<IRepository<BotProject, BotProjectId>>();
        projects.GetByIdAsync(Arg.Any<BotProjectId>(), Arg.Any<CancellationToken>()).Returns(project);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        return (projects, unitOfWork, new AddStepRouteCommandHandler(projects, unitOfWork));
    }

    [Fact]
    public async Task Returns_NotFound_when_the_project_does_not_exist()
    {
        var (_, _, handler) = CreateHandler(null);

        var result = await handler.Handle(
            new AddStepRouteCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ConditionType.NoCondition, [], StepRouteType.EndConversation, null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BotProject.NotFound");
    }

    [Fact]
    public async Task Adds_a_next_step_route_targeting_a_step_in_the_same_flow()
    {
        var project = CreateProjectWithFlowAndStep(out var flowId, out var stepId);
        var flow = project.FindFlow(flowId)!;
        var nextStep = flow.AddStep(StepType.SendMessage, "Great, let's begin.", order: 1).Value;
        var (_, unitOfWork, handler) = CreateHandler(project);

        var result = await handler.Handle(
            new AddStepRouteCommand(project.Id.Value, flowId, stepId, ConditionType.HasOnlyKeyword, ["Yes"], StepRouteType.NextStep, null, nextStep.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var step = flow.FindStep(stepId)!;
        step.Routes.Should().ContainSingle(r => r.Id == result.Value && r.RouteType == StepRouteType.NextStep && r.TargetStepId == nextStep.Id);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_a_next_step_route_whose_target_is_not_in_the_same_flow()
    {
        var project = CreateProjectWithFlowAndStep(out var flowId, out var stepId);
        var otherFlow = project.AddFlow("Other Flow", isDefault: false).Value;
        var stepInOtherFlow = otherFlow.AddStep(StepType.SendMessage, "Elsewhere.", order: 0).Value;
        var (_, unitOfWork, handler) = CreateHandler(project);

        var result = await handler.Handle(
            new AddStepRouteCommand(project.Id.Value, flowId, stepId, ConditionType.HasOnlyKeyword, ["Yes"], StepRouteType.NextStep, null, stepInOtherFlow.Id),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Flow.TargetStepNotFound");
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Adds_a_switch_flow_route_targeting_a_step_in_a_different_flow()
    {
        var project = CreateProjectWithFlowAndStep(out var flowId, out var stepId);
        var otherFlow = project.AddFlow("Follow-up Flow", isDefault: false).Value;
        var stepInOtherFlow = otherFlow.AddStep(StepType.SendMessage, "Welcome to follow-up.", order: 0).Value;
        var (_, unitOfWork, handler) = CreateHandler(project);

        var result = await handler.Handle(
            new AddStepRouteCommand(
                project.Id.Value, flowId, stepId, ConditionType.NoCondition, [], StepRouteType.SwitchFlow, otherFlow.Id, stepInOtherFlow.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var step = project.FindFlow(flowId)!.FindStep(stepId)!;
        step.Routes.Should().ContainSingle(r =>
            r.Id == result.Value && r.RouteType == StepRouteType.SwitchFlow && r.TargetFlowId == otherFlow.Id && r.TargetStepId == stepInOtherFlow.Id);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_a_switch_flow_route_targeting_an_unknown_flow()
    {
        var project = CreateProjectWithFlowAndStep(out var flowId, out var stepId);
        var (_, unitOfWork, handler) = CreateHandler(project);

        var result = await handler.Handle(
            new AddStepRouteCommand(
                project.Id.Value, flowId, stepId, ConditionType.NoCondition, [], StepRouteType.SwitchFlow, Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BotProject.TargetFlowNotFound");
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Adds_an_end_conversation_route_with_no_target()
    {
        var project = CreateProjectWithFlowAndStep(out var flowId, out var stepId);
        var (_, unitOfWork, handler) = CreateHandler(project);

        var result = await handler.Handle(
            new AddStepRouteCommand(project.Id.Value, flowId, stepId, ConditionType.HasOnlyKeyword, ["No"], StepRouteType.EndConversation, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var step = project.FindFlow(flowId)!.FindStep(stepId)!;
        step.Routes.Should().ContainSingle(r => r.Id == result.Value && r.RouteType == StepRouteType.EndConversation);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(ConditionType.HasOnlyKeyword, new string[] { })]
    [InlineData(ConditionType.HasOnlyKeyword, new[] { "Yes", "No" })]
    [InlineData(ConditionType.HasAnyKeywords, new string[] { })]
    public async Task Rejects_keyword_counts_the_condition_type_could_never_match(ConditionType conditionType, string[] keywords)
    {
        var validator = new AddStepRouteCommandValidator();

        var result = await validator.ValidateAsync(
            new AddStepRouteCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), conditionType, keywords, StepRouteType.EndConversation, null, null));

        result.IsValid.Should().BeFalse();
    }
}
