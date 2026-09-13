using FluentAssertions;
using Peoplise.Modules.ATS.Domain.Aggregates;
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
}
