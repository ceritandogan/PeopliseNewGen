using FluentAssertions;
using Peoplise.Modules.HrBot.Domain.Aggregates;
using Xunit;

namespace Peoplise.Modules.HrBot.Tests.Domain;

public class BotProjectTests
{
    [Fact]
    public void AddFlow_allows_exactly_one_default_flow()
    {
        var project = BotProject.Create("Backend Screening Bot", Guid.NewGuid());
        project.AddFlow("Main Flow", isDefault: true);

        var result = project.AddFlow("Second Default Attempt", isDefault: true);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BotProject.DefaultFlowAlreadySet");
    }

    [Fact]
    public void Multiple_non_default_flows_are_allowed()
    {
        var project = BotProject.Create("Backend Screening Bot", Guid.NewGuid());
        project.AddFlow("Main Flow", isDefault: true);

        var result = project.AddFlow("Follow-up Flow", isDefault: false);

        result.IsSuccess.Should().BeTrue();
        project.Flows.Should().HaveCount(2);
    }

    [Fact]
    public void A_new_project_starts_with_an_empty_knowledgebase()
    {
        var project = BotProject.Create("Backend Screening Bot", Guid.NewGuid());

        project.Knowledgebase.Questions.Should().BeEmpty();
    }
}
