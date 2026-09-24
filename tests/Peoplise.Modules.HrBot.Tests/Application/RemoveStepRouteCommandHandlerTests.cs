using FluentAssertions;
using NSubstitute;
using Peoplise.Modules.HrBot.Application.BotProjects.Commands;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.Entities;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Xunit;

namespace Peoplise.Modules.HrBot.Tests.Application;

public class RemoveStepRouteCommandHandlerTests
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

    [Fact]
    public async Task Returns_NotFound_when_the_project_does_not_exist()
    {
        var projects = Substitute.For<IRepository<BotProject, BotProjectId>>();
        projects.GetByIdAsync(Arg.Any<BotProjectId>(), Arg.Any<CancellationToken>()).Returns((BotProject?)null);
        var handler = new RemoveStepRouteCommandHandler(projects, Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(
            new RemoveStepRouteCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BotProject.NotFound");
    }

    [Fact]
    public async Task Removes_an_existing_route_and_saves()
    {
        var project = CreateProjectWithFlowAndStep(out var flowId, out var stepId);
        var step = project.FindFlow(flowId)!.FindStep(stepId)!;
        var route = StepRoute.EndConversation(ConditionType.HasOnlyKeyword, ["No"]);
        step.AddRoute(route);
        var projects = Substitute.For<IRepository<BotProject, BotProjectId>>();
        projects.GetByIdAsync(Arg.Any<BotProjectId>(), Arg.Any<CancellationToken>()).Returns(project);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new RemoveStepRouteCommandHandler(projects, unitOfWork);

        var result = await handler.Handle(
            new RemoveStepRouteCommand(project.Id.Value, flowId, stepId, route.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        step.Routes.Should().BeEmpty();
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_RouteNotFound_without_saving_for_an_unknown_route()
    {
        var project = CreateProjectWithFlowAndStep(out var flowId, out var stepId);
        var projects = Substitute.For<IRepository<BotProject, BotProjectId>>();
        projects.GetByIdAsync(Arg.Any<BotProjectId>(), Arg.Any<CancellationToken>()).Returns(project);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new RemoveStepRouteCommandHandler(projects, unitOfWork);

        var result = await handler.Handle(
            new RemoveStepRouteCommand(project.Id.Value, flowId, stepId, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Step.RouteNotFound");
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
