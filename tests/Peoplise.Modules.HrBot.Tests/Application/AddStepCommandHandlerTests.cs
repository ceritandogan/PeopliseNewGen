using FluentAssertions;
using FluentValidation;
using NSubstitute;
using Peoplise.Modules.HrBot.Application.BotProjects.Commands;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Xunit;

namespace Peoplise.Modules.HrBot.Tests.Application;

public class AddStepCommandHandlerTests
{
    private static BotProject CreateProjectWithFlow(out Guid flowId)
    {
        var project = BotProject.Create("Backend Screening Bot", Guid.NewGuid(), retentionPeriodDays: 90).Value;
        var flow = project.AddFlow("Pre-screening", isDefault: true).Value;
        flowId = flow.Id;
        return project;
    }

    [Fact]
    public async Task Returns_NotFound_when_the_project_does_not_exist()
    {
        var projects = Substitute.For<IRepository<BotProject, BotProjectId>>();
        projects.GetByIdAsync(Arg.Any<BotProjectId>(), Arg.Any<CancellationToken>()).Returns((BotProject?)null);
        var handler = new AddStepCommandHandler(projects, Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(
            new AddStepCommand(Guid.NewGuid(), Guid.NewGuid(), StepType.SendMessage, "Hello!", 0, null, null, false, false),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BotProject.NotFound");
    }

    [Fact]
    public async Task Returns_FlowNotFound_when_the_flow_is_not_part_of_the_project()
    {
        var project = CreateProjectWithFlow(out _);
        var projects = Substitute.For<IRepository<BotProject, BotProjectId>>();
        projects.GetByIdAsync(Arg.Any<BotProjectId>(), Arg.Any<CancellationToken>()).Returns(project);
        var handler = new AddStepCommandHandler(projects, Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(
            new AddStepCommand(project.Id.Value, Guid.NewGuid(), StepType.SendMessage, "Hello!", 0, null, null, false, false),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BotProject.FlowNotFound");
    }

    [Fact]
    public async Task Adds_a_step_and_saves()
    {
        var project = CreateProjectWithFlow(out var flowId);
        var projects = Substitute.For<IRepository<BotProject, BotProjectId>>();
        projects.GetByIdAsync(Arg.Any<BotProjectId>(), Arg.Any<CancellationToken>()).Returns(project);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new AddStepCommandHandler(projects, unitOfWork);

        var result = await handler.Handle(
            new AddStepCommand(project.Id.Value, flowId, StepType.SendQuickReply, "Ready to continue?", 0, ["Yes", "No"], null, false, false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var flow = project.FindFlow(flowId)!;
        var expectedOptions = new[] { "Yes", "No" };
        flow.Steps.Should().ContainSingle(s => s.Id == result.Value && s.Content == "Ready to continue?" && s.QuickReplyOptions.SequenceEqual(expectedOptions));
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_a_screen_out_step_that_is_not_also_final_without_saving()
    {
        var project = CreateProjectWithFlow(out var flowId);
        var projects = Substitute.For<IRepository<BotProject, BotProjectId>>();
        projects.GetByIdAsync(Arg.Any<BotProjectId>(), Arg.Any<CancellationToken>()).Returns(project);
        var validator = new AddStepCommandValidator();

        var result = await validator.ValidateAsync(
            new AddStepCommand(project.Id.Value, flowId, StepType.SendMessage, "Sorry, not a fit.", 0, null, null, false, true));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Rejects_a_duplicate_step_order_without_saving()
    {
        var project = CreateProjectWithFlow(out var flowId);
        project.FindFlow(flowId)!.AddStep(StepType.SendMessage, "Welcome!", order: 0);
        var projects = Substitute.For<IRepository<BotProject, BotProjectId>>();
        projects.GetByIdAsync(Arg.Any<BotProjectId>(), Arg.Any<CancellationToken>()).Returns(project);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new AddStepCommandHandler(projects, unitOfWork);

        var result = await handler.Handle(
            new AddStepCommand(project.Id.Value, flowId, StepType.SendMessage, "Also welcome!", 0, null, null, false, false),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Flow.DuplicateStepOrder");
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
