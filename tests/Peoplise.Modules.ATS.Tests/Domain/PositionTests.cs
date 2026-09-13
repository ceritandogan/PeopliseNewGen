using FluentAssertions;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Xunit;

namespace Peoplise.Modules.ATS.Tests.Domain;

public class PositionTests
{
    private static Position CreatePosition() =>
        Position.Create("Senior Backend Engineer", "Engineering", "Istanbul", "Turkey",
            WorkMode.Hybrid, SeniorityLevel.Senior, EmploymentType.FullTime);

    [Fact]
    public void Create_rejects_a_blank_title()
    {
        var act = () => Position.Create("", "Engineering", "Istanbul", "Turkey",
            WorkMode.Hybrid, SeniorityLevel.Senior, EmploymentType.FullTime);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddTeamMember_succeeds_for_a_new_user()
    {
        var position = CreatePosition();

        var result = position.AddTeamMember("user-1", PositionRole.Manager);

        result.IsSuccess.Should().BeTrue();
        position.TeamMembers.Should().ContainSingle(m => m.UserId == "user-1" && m.Role == PositionRole.Manager);
    }

    [Fact]
    public void AddTeamMember_rejects_a_user_already_on_the_team()
    {
        var position = CreatePosition();
        position.AddTeamMember("user-1", PositionRole.Evaluator);

        var result = position.AddTeamMember("user-1", PositionRole.Manager);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Position.TeamMemberAlreadyAdded");
        position.TeamMembers.Should().ContainSingle(); // the second add must not have been applied
    }

    [Fact]
    public void AssignWorkflow_sets_the_workflow_definition_reference()
    {
        var position = CreatePosition();
        var workflowId = WorkflowDefinitionId.New();

        position.AssignWorkflow(workflowId);

        position.WorkflowDefinitionId.Should().Be(workflowId);
    }
}
