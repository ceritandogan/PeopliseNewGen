using FluentAssertions;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.Events;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Xunit;

namespace Peoplise.Modules.VideoInterview.Tests.Domain;

public class CaseVideoAnswerTests
{
    private static Case CreateCase() =>
        Case.Start(CaseBotProjectId.New(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

    [Fact]
    public void The_first_recording_at_a_step_does_not_consume_a_retake()
    {
        var @case = CreateCase();
        var stepId = Guid.NewGuid();

        var result = @case.RecordVideoAnswer(stepId, "local-storage://take1.mp4", retakesAllowed: 0, DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeTrue();
        @case.RetakeCounts.Should().BeEmpty();
        @case.StepConversations.Should().ContainSingle(c => c.VideoUrl == "local-storage://take1.mp4");
    }

    [Fact]
    public void RecordVideoAnswer_raises_a_VideoRecordedEvent()
    {
        var @case = CreateCase();

        @case.RecordVideoAnswer(Guid.NewGuid(), "local-storage://take1.mp4", retakesAllowed: 1, DateTimeOffset.UtcNow);

        @case.DomainEvents.Should().Contain(e => e is VideoRecordedEvent);
    }

    [Fact]
    public void A_retake_is_rejected_when_the_projects_policy_allows_zero_retakes()
    {
        var @case = CreateCase();
        var stepId = Guid.NewGuid();
        @case.RecordVideoAnswer(stepId, "local-storage://take1.mp4", retakesAllowed: 0, DateTimeOffset.UtcNow);

        var result = @case.RecordVideoAnswer(stepId, "local-storage://take2.mp4", retakesAllowed: 0, DateTimeOffset.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Case.RetakeLimitExceeded");
        @case.StepConversations.Should().ContainSingle(c => c.VideoUrl == "local-storage://take1.mp4");
    }

    [Fact]
    public void A_retake_within_the_allowed_limit_replaces_the_previous_take()
    {
        var @case = CreateCase();
        var stepId = Guid.NewGuid();
        @case.RecordVideoAnswer(stepId, "local-storage://take1.mp4", retakesAllowed: 1, DateTimeOffset.UtcNow);

        var result = @case.RecordVideoAnswer(stepId, "local-storage://take2.mp4", retakesAllowed: 1, DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeTrue();
        @case.StepConversations.Should().ContainSingle(c => c.VideoUrl == "local-storage://take2.mp4");
        @case.RetakeCounts.Should().ContainSingle(r => r.StepId == stepId && r.RetakesUsed == 1);
    }

    [Fact]
    public void A_second_retake_beyond_the_allowed_limit_of_one_is_rejected()
    {
        var @case = CreateCase();
        var stepId = Guid.NewGuid();
        @case.RecordVideoAnswer(stepId, "local-storage://take1.mp4", retakesAllowed: 1, DateTimeOffset.UtcNow);
        @case.RecordVideoAnswer(stepId, "local-storage://take2.mp4", retakesAllowed: 1, DateTimeOffset.UtcNow);

        var result = @case.RecordVideoAnswer(stepId, "local-storage://take3.mp4", retakesAllowed: 1, DateTimeOffset.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Case.RetakeLimitExceeded");
        @case.StepConversations.Should().ContainSingle(c => c.VideoUrl == "local-storage://take2.mp4");
    }

    [Fact]
    public void Retakes_at_different_steps_are_tracked_independently()
    {
        var @case = CreateCase();
        var stepA = Guid.NewGuid();
        var stepB = Guid.NewGuid();
        @case.RecordVideoAnswer(stepA, "local-storage://a1.mp4", retakesAllowed: 1, DateTimeOffset.UtcNow);
        @case.RecordVideoAnswer(stepA, "local-storage://a2.mp4", retakesAllowed: 1, DateTimeOffset.UtcNow);

        // stepB's first take, even though stepA has used its one retake already.
        var result = @case.RecordVideoAnswer(stepB, "local-storage://b1.mp4", retakesAllowed: 1, DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeTrue();
    }
}
