using FluentAssertions;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.Entities;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Xunit;

namespace Peoplise.Modules.ATS.Tests.Domain;

public class WorkflowDefinitionTests
{
    [Fact]
    public void AddStage_succeeds_and_the_stage_is_retrievable_in_order()
    {
        var definition = WorkflowDefinition.Create("Standard Engineering Flow");

        definition.AddStage("Application Form", StageType.InformationForm, order: 0);
        definition.AddStage("Live Interview", StageType.LiveInterview, order: 1);

        definition.Stages.Should().HaveCount(2);
        definition.Stages.Select(s => s.Name).Should().ContainInOrder("Application Form", "Live Interview");
    }

    [Fact]
    public void AddStage_rejects_a_negative_order()
    {
        var definition = WorkflowDefinition.Create("Standard Flow");

        var result = definition.AddStage("Bad Stage", StageType.InformationForm, order: -1);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WorkflowDefinition.InvalidOrder");
    }

    [Fact]
    public void AddStage_rejects_a_duplicate_stage_type_at_the_same_order()
    {
        var definition = WorkflowDefinition.Create("Standard Flow");
        definition.AddStage("Screening Test", StageType.ScreeningTest, order: 0);

        var result = definition.AddStage("Another Screening Test", StageType.ScreeningTest, order: 0);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WorkflowDefinition.DuplicateStage");
        definition.Stages.Should().ContainSingle();
    }

    [Fact]
    public void Two_different_stage_types_may_share_the_same_order_to_express_parallelism()
    {
        var definition = WorkflowDefinition.Create("Standard Flow");

        var first = definition.AddStage("Document Upload", StageType.DocumentCollection, order: 1);
        var second = definition.AddStage("Survey", StageType.ScreeningTest, order: 1);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        definition.Stages.Should().HaveCount(2);
    }

    [Fact]
    public void ReorderStages_replaces_every_stage_order_with_its_index_in_the_given_sequence()
    {
        var definition = WorkflowDefinition.Create("Standard Flow");
        var first = definition.AddStage("Application Form", StageType.InformationForm, order: 0).Value;
        var second = definition.AddStage("Screening Test", StageType.ScreeningTest, order: 1).Value;
        var third = definition.AddStage("Live Interview", StageType.LiveInterview, order: 2).Value;

        var result = definition.ReorderStages([third.Id, first.Id, second.Id]);

        result.IsSuccess.Should().BeTrue();
        definition.Stages.Select(s => s.Name).Should().ContainInOrder("Live Interview", "Application Form", "Screening Test");
    }

    [Fact]
    public void ReorderStages_rejects_a_list_missing_a_current_stage()
    {
        var definition = WorkflowDefinition.Create("Standard Flow");
        var first = definition.AddStage("Application Form", StageType.InformationForm, order: 0).Value;
        definition.AddStage("Screening Test", StageType.ScreeningTest, order: 1);

        var result = definition.ReorderStages([first.Id]);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WorkflowDefinition.InvalidReorder");
    }

    [Fact]
    public void ReorderStages_rejects_an_id_that_is_not_part_of_this_workflow()
    {
        var definition = WorkflowDefinition.Create("Standard Flow");
        var first = definition.AddStage("Application Form", StageType.InformationForm, order: 0).Value;
        definition.AddStage("Screening Test", StageType.ScreeningTest, order: 1);

        // Same length as the current stage list (2), so the count/uniqueness check
        // passes and this actually exercises the UnknownStage membership check below it.
        var result = definition.ReorderStages([first.Id, Guid.NewGuid()]);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WorkflowDefinition.UnknownStage");
    }

    [Fact]
    public void RemoveStage_removes_an_existing_stage()
    {
        var definition = WorkflowDefinition.Create("Standard Flow");
        var stage = definition.AddStage("Application Form", StageType.InformationForm, order: 0).Value;

        var result = definition.RemoveStage(stage.Id);

        result.IsSuccess.Should().BeTrue();
        definition.Stages.Should().BeEmpty();
    }

    [Fact]
    public void RemoveStage_fails_for_an_unknown_stage_id()
    {
        var definition = WorkflowDefinition.Create("Standard Flow");

        var result = definition.RemoveStage(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WorkflowDefinition.StageNotFound");
    }

    [Fact]
    public void A_stage_rule_can_be_added_and_removed()
    {
        var definition = WorkflowDefinition.Create("Standard Flow");
        var stage = definition.AddStage("Screening Test", StageType.ScreeningTest, order: 0).Value;
        var rule = StageRule.AdvanceIfScoreAtLeast(75m);

        stage.AddRule(rule);
        stage.Rules.Should().ContainSingle(r => r.Id == rule.Id);

        var result = stage.RemoveRule(rule.Id);

        result.IsSuccess.Should().BeTrue();
        stage.Rules.Should().BeEmpty();
    }

    [Fact]
    public void RemoveRule_fails_for_an_unknown_rule_id()
    {
        var definition = WorkflowDefinition.Create("Standard Flow");
        var stage = definition.AddStage("Screening Test", StageType.ScreeningTest, order: 0).Value;

        var result = stage.RemoveRule(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Stage.RuleNotFound");
    }
}
