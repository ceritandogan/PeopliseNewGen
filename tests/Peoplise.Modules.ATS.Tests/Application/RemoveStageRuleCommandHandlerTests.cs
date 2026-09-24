using FluentAssertions;
using NSubstitute;
using Peoplise.Modules.ATS.Application.Workflows.Commands;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.Entities;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Xunit;

namespace Peoplise.Modules.ATS.Tests.Application;

public class RemoveStageRuleCommandHandlerTests
{
    [Fact]
    public async Task Returns_NotFound_when_the_workflow_does_not_exist()
    {
        var repository = Substitute.For<IRepository<WorkflowDefinition, WorkflowDefinitionId>>();
        repository.GetByIdAsync(Arg.Any<WorkflowDefinitionId>(), Arg.Any<CancellationToken>()).Returns((WorkflowDefinition?)null);
        var handler = new RemoveStageRuleCommandHandler(repository, Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(
            new RemoveStageRuleCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WorkflowDefinition.NotFound");
    }

    [Fact]
    public async Task Returns_StageNotFound_when_the_stage_is_not_part_of_the_workflow()
    {
        var workflow = WorkflowDefinition.Create("Standard Flow");
        var repository = Substitute.For<IRepository<WorkflowDefinition, WorkflowDefinitionId>>();
        repository.GetByIdAsync(Arg.Any<WorkflowDefinitionId>(), Arg.Any<CancellationToken>()).Returns(workflow);
        var handler = new RemoveStageRuleCommandHandler(repository, Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(
            new RemoveStageRuleCommand(workflow.Id.Value, Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WorkflowDefinition.StageNotFound");
    }

    [Fact]
    public async Task Removes_an_existing_rule_and_saves()
    {
        var workflow = WorkflowDefinition.Create("Standard Flow");
        var stage = workflow.AddStage("Screening Test", StageType.ScreeningTest, order: 0).Value;
        var rule = StageRule.EliminateIfScoreBelow(40m);
        stage.AddRule(rule);
        var repository = Substitute.For<IRepository<WorkflowDefinition, WorkflowDefinitionId>>();
        repository.GetByIdAsync(Arg.Any<WorkflowDefinitionId>(), Arg.Any<CancellationToken>()).Returns(workflow);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new RemoveStageRuleCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(
            new RemoveStageRuleCommand(workflow.Id.Value, stage.Id, rule.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        stage.Rules.Should().BeEmpty();
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_RuleNotFound_without_saving_for_an_unknown_rule()
    {
        var workflow = WorkflowDefinition.Create("Standard Flow");
        var stage = workflow.AddStage("Screening Test", StageType.ScreeningTest, order: 0).Value;
        var repository = Substitute.For<IRepository<WorkflowDefinition, WorkflowDefinitionId>>();
        repository.GetByIdAsync(Arg.Any<WorkflowDefinitionId>(), Arg.Any<CancellationToken>()).Returns(workflow);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new RemoveStageRuleCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(
            new RemoveStageRuleCommand(workflow.Id.Value, stage.Id, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Stage.RuleNotFound");
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
