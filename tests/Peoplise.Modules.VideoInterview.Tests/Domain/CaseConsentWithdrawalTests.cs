using FluentAssertions;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.Events;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Xunit;

namespace Peoplise.Modules.VideoInterview.Tests.Domain;

public class CaseConsentWithdrawalTests
{
    [Fact]
    public void WithdrawConsent_clears_every_step_conversations_media_references()
    {
        var @case = Case.Start(CaseBotProjectId.New(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var videoStepId = Guid.NewGuid();
        var docStepId = Guid.NewGuid();
        @case.RecordVideoAnswer(videoStepId, "local-storage://video.mp4", retakesAllowed: 0, DateTimeOffset.UtcNow);
        @case.RecordDocumentUpload(docStepId, "local-storage://resume.pdf", DateTimeOffset.UtcNow);

        var result = @case.WithdrawConsent("Candidate requested deletion.", DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeTrue();
        @case.StepConversations.Should().OnlyContain(c => c.VideoUrl == null && c.DocumentUrl == null && c.TranscriptText == null);
    }

    [Fact]
    public void WithdrawConsent_anonymizes_the_candidate_identity_but_keeps_scorings_for_statistics()
    {
        var @case = Case.Start(CaseBotProjectId.New(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var competencyId = Guid.NewGuid();
        @case.SubmitScoring("reviewer-1", Guid.NewGuid(), competencyId, 85, DateTimeOffset.UtcNow);

        @case.WithdrawConsent("Candidate requested deletion.", DateTimeOffset.UtcNow);

        @case.CandidateId.Should().Be(Guid.Empty, "identity must be severed");
        @case.Scorings.Should().ContainSingle(s => s.Score == 85, "scoring data must survive for aggregate statistics");
    }

    [Fact]
    public void WithdrawConsent_raises_a_ConsentWithdrawnEvent_and_closes_the_case()
    {
        var @case = Case.Start(CaseBotProjectId.New(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        var result = @case.WithdrawConsent("No longer interested.", DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeTrue();
        @case.Status.Should().Be(CaseStatus.ConsentWithdrawn);
        @case.IsOpen.Should().BeFalse();
        @case.DomainEvents.Should().Contain(e => e is ConsentWithdrawnEvent);
    }

    [Fact]
    public void ExpireRetention_does_the_same_anonymization_and_raises_a_different_event()
    {
        var @case = Case.Start(CaseBotProjectId.New(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        @case.RecordVideoAnswer(Guid.NewGuid(), "local-storage://video.mp4", retakesAllowed: 0, DateTimeOffset.UtcNow);

        var result = @case.ExpireRetention(DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeTrue();
        @case.Status.Should().Be(CaseStatus.RetentionExpired);
        @case.CandidateId.Should().Be(Guid.Empty);
        @case.StepConversations.Should().OnlyContain(c => c.VideoUrl == null);
        @case.DomainEvents.Should().Contain(e => e is DataRetentionExpiredEvent);
    }

    [Fact]
    public void A_closed_case_cannot_withdraw_consent_twice()
    {
        var @case = Case.Start(CaseBotProjectId.New(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        @case.WithdrawConsent("First.", DateTimeOffset.UtcNow);

        var result = @case.WithdrawConsent("Second.", DateTimeOffset.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Case.AlreadyClosed");
    }

    [Fact]
    public void ExpireRetention_runs_on_an_already_completed_case()
    {
        var @case = Case.Start(CaseBotProjectId.New(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        @case.RecordVideoAnswer(Guid.NewGuid(), "local-storage://video.mp4", retakesAllowed: 0, DateTimeOffset.UtcNow);
        @case.Complete(DateTimeOffset.UtcNow);

        var result = @case.ExpireRetention(DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeTrue("a retention sweep's main target is data that aged out after the interview finished");
        @case.Status.Should().Be(CaseStatus.RetentionExpired);
        @case.CandidateId.Should().Be(Guid.Empty);
        @case.StepConversations.Should().OnlyContain(c => c.VideoUrl == null);
    }

    [Fact]
    public void ExpireRetention_preserves_the_original_completion_date_of_an_already_completed_case()
    {
        var @case = Case.Start(CaseBotProjectId.New(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var completedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        @case.Complete(completedAt);

        @case.ExpireRetention(completedAt.AddDays(30));

        @case.CompletedAt.Should().Be(completedAt, "reports/statistics rely on the true completion date surviving anonymization");
    }

    [Fact]
    public void ExpireRetention_sets_CompletedAt_for_a_case_that_was_never_completed()
    {
        var @case = Case.Start(CaseBotProjectId.New(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var expiredAt = DateTimeOffset.UtcNow;

        @case.ExpireRetention(expiredAt);

        @case.CompletedAt.Should().Be(expiredAt);
    }

    [Fact]
    public void ExpireRetention_cannot_run_twice()
    {
        var @case = Case.Start(CaseBotProjectId.New(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        @case.ExpireRetention(DateTimeOffset.UtcNow);

        var result = @case.ExpireRetention(DateTimeOffset.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Case.AlreadyAnonymized");
    }

    [Fact]
    public void ExpireRetention_cannot_run_on_a_case_that_already_withdrew_consent()
    {
        var @case = Case.Start(CaseBotProjectId.New(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        @case.WithdrawConsent("Candidate requested deletion.", DateTimeOffset.UtcNow);

        var result = @case.ExpireRetention(DateTimeOffset.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Case.AlreadyAnonymized");
    }
}
