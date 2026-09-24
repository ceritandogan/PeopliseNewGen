using FluentAssertions;
using NSubstitute;
using Peoplise.Modules.ATS.Application.Workflows.Commands;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Xunit;

namespace Peoplise.Modules.ATS.Tests.Application;

public class AddStageRuleCommandHandlerTests
{
    [Fact]
    public async Task Returns_NotFound_when_the_workflow_does_not_exist()
    {
        var repository = Substitute.For<IRepository<WorkflowDefinition, WorkflowDefinitionId>>();
        repository.GetByIdAsync(Arg.Any<WorkflowDefinitionId>(), Arg.Any<CancellationToken>()).Returns((WorkflowDefinition?)null);
        var handler = new AddStageRuleCommandHandler(repository, Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(
            new AddStageRuleCommand(Guid.NewGuid(), Guid.NewGuid(), StageRuleType.AdvanceIfScoreAtLeast, 75m, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WorkflowDefinition.NotFound");
    }

    [Fact]
    public async Task Returns_StageNotFound_when_the_stage_is_not_part_of_the_workflow()
    {
        var workflow = WorkflowDefinition.Create("Standard Flow");
        var repository = Substitute.For<IRepository<WorkflowDefinition, WorkflowDefinitionId>>();
        repository.GetByIdAsync(Arg.Any<WorkflowDefinitionId>(), Arg.Any<CancellationToken>()).Returns(workflow);
        var handler = new AddStageRuleCommandHandler(repository, Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(
            new AddStageRuleCommand(workflow.Id.Value, Guid.NewGuid(), StageRuleType.AdvanceIfScoreAtLeast, 75m, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WorkflowDefinition.StageNotFound");
    }

    [Fact]
    public async Task Adds_an_AdvanceIfScoreAtLeast_rule_with_its_threshold()
    {
        var workflow = WorkflowDefinition.Create("Standard Flow");
        var stage = workflow.AddStage("Screening Test", StageType.ScreeningTest, order: 0).Value;
        var repository = Substitute.For<IRepository<WorkflowDefinition, WorkflowDefinitionId>>();
        repository.GetByIdAsync(Arg.Any<WorkflowDefinitionId>(), Arg.Any<CancellationToken>()).Returns(workflow);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new AddStageRuleCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(
            new AddStageRuleCommand(workflow.Id.Value, stage.Id, StageRuleType.AdvanceIfScoreAtLeast, 80m, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        stage.Rules.Should().ContainSingle(r => r.Type == StageRuleType.AdvanceIfScoreAtLeast && r.Threshold == 80m);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Adds_an_ActivateAfterDelay_rule_with_its_delay_days()
    {
        var workflow = WorkflowDefinition.Create("Standard Flow");
        var stage = workflow.AddStage("Application Form", StageType.InformationForm, order: 0).Value;
        var repository = Substitute.For<IRepository<WorkflowDefinition, WorkflowDefinitionId>>();
        repository.GetByIdAsync(Arg.Any<WorkflowDefinitionId>(), Arg.Any<CancellationToken>()).Returns(workflow);
        var handler = new AddStageRuleCommandHandler(repository, Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(
            new AddStageRuleCommand(workflow.Id.Value, stage.Id, StageRuleType.ActivateAfterDelay, null, 5),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        stage.Rules.Should().ContainSingle(r => r.Type == StageRuleType.ActivateAfterDelay && r.DelayDays == 5);
    }
}
