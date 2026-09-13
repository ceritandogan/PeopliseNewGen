using FluentAssertions;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Xunit;

namespace Peoplise.Modules.VideoInterview.Tests.Domain;

public class CaseCompetencyScoringTests
{
    private static Case CreateCase() =>
        Case.Start(CaseBotProjectId.New(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

    [Fact]
    public void CalculateCompetencyResult_fails_when_no_scorings_exist_for_the_competency()
    {
        var @case = CreateCase();

        var result = @case.CalculateCompetencyResult(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Case.NoScoringsYet");
    }

    [Fact]
    public void CalculateCompetencyResult_averages_equal_weight_scorings()
    {
        var @case = CreateCase();
        var competencyId = Guid.NewGuid();
        var stepId = Guid.NewGuid();

        @case.SubmitScoring("reviewer-1", stepId, competencyId, 80, DateTimeOffset.UtcNow);
        @case.SubmitScoring("reviewer-2", stepId, competencyId, 60, DateTimeOffset.UtcNow);

        var result = @case.CalculateCompetencyResult(competencyId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(70m);
    }

    [Fact]
    public void CalculateCompetencyResult_weights_partial_contributions_correctly()
    {
        // "kısmi puan, ağırlıklı ortalama": a scoring worth double the weight of
        // another must pull the average toward it proportionally, not count as a
        // simple unweighted average.
        var @case = CreateCase();
        var competencyId = Guid.NewGuid();
        var stepId = Guid.NewGuid();

        @case.SubmitScoring("reviewer-1", stepId, competencyId, 100, DateTimeOffset.UtcNow, weight: 2m);
        @case.SubmitScoring("reviewer-2", stepId, competencyId, 40, DateTimeOffset.UtcNow, weight: 1m);

        // (100*2 + 40*1) / (2+1) = 240/3 = 80
        var result = @case.CalculateCompetencyResult(competencyId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(80m);
    }

    [Fact]
    public void SubmitScoring_rejects_a_duplicate_from_the_same_reviewer_for_the_same_step_and_competency()
    {
        var @case = CreateCase();
        var competencyId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        @case.SubmitScoring("reviewer-1", stepId, competencyId, 80, DateTimeOffset.UtcNow);

        var result = @case.SubmitScoring("reviewer-1", stepId, competencyId, 90, DateTimeOffset.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Case.DuplicateScoring");
        @case.Scorings.Should().ContainSingle();
    }

    [Fact]
    public void Complete_computes_a_CompetencyResult_for_every_scored_competency()
    {
        var @case = CreateCase();
        var stepId = Guid.NewGuid();
        var competencyA = Guid.NewGuid();
        var competencyB = Guid.NewGuid();
        @case.SubmitScoring("reviewer-1", stepId, competencyA, 80, DateTimeOffset.UtcNow);
        @case.SubmitScoring("reviewer-1", stepId, competencyB, 60, DateTimeOffset.UtcNow);

        var result = @case.Complete(DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeTrue();
        @case.AssessmentResult.Should().NotBeNull();
        @case.AssessmentResult!.CompetencyResults.Should().HaveCount(2);
        @case.AssessmentResult.CompetencyResults.Should().Contain(r => r.CompetencyId == competencyA && r.Score == 80m);
    }
}
