using FluentAssertions;
using Peoplise.Modules.HrBot.Domain.Entities;
using Peoplise.Modules.HrBot.Domain.ValueObjects;
using Xunit;

namespace Peoplise.Modules.HrBot.Tests.Domain;

public class StepTests
{
    [Fact]
    public void Only_a_final_step_can_be_a_screen_out_step()
    {
        var act = () => new Step(Guid.NewGuid(), 0, StepType.SendMessage, "Sorry, not a fit.", isFinalStep: false, isScreenOut: true);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void A_route_can_be_added_and_removed()
    {
        var step = new Step(Guid.NewGuid(), 0, StepType.SendQuickReply, "Ready?", quickReplyOptions: ["Yes", "No"]);
        var route = StepRoute.EndConversation(ConditionType.HasOnlyKeyword, ["No"]);

        step.AddRoute(route);
        step.Routes.Should().ContainSingle(r => r.Id == route.Id);

        var result = step.RemoveRoute(route.Id);

        result.IsSuccess.Should().BeTrue();
        step.Routes.Should().BeEmpty();
    }

    [Fact]
    public void RemoveRoute_fails_for_an_unknown_route_id()
    {
        var step = new Step(Guid.NewGuid(), 0, StepType.SendMessage, "Welcome!");

        var result = step.RemoveRoute(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Step.RouteNotFound");
    }
}
