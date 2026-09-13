using FluentAssertions;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.Entities;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Xunit;

namespace Peoplise.Modules.VideoInterview.Tests.Domain;

public class ReportGenerationTests
{
    [Fact]
    public void GenerateReport_produces_one_section_per_template_section_in_order()
    {
        var @case = Case.Start(CaseBotProjectId.New(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var template = new ReportTemplate(Guid.NewGuid(), "Standard Report");
        template.AddSection("Summary", order: 0);
        template.AddSection("Competency Scores", order: 1);
        template.AddSection("Recommendations", order: 2);

        var report = @case.GenerateReport(template, DateTimeOffset.UtcNow);

        report.Sections.Should().HaveCount(3);
        report.Sections.Select(s => s.Title).Should().BeEquivalentTo(["Summary", "Competency Scores", "Recommendations"]);
    }

    [Fact]
    public void A_competency_section_is_filled_with_the_computed_scores_when_the_case_has_a_result()
    {
        var @case = Case.Start(CaseBotProjectId.New(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var competencyId = Guid.NewGuid();
        @case.SubmitScoring("reviewer-1", Guid.NewGuid(), competencyId, 90, DateTimeOffset.UtcNow);
        @case.Complete(DateTimeOffset.UtcNow);

        var template = new ReportTemplate(Guid.NewGuid(), "Standard Report");
        template.AddSection("Competency Scores", order: 0);

        var report = @case.GenerateReport(template, DateTimeOffset.UtcNow);

        var section = report.Sections.Single();
        section.Content.Should().Contain(competencyId.ToString());
        section.Content.Should().Contain("90");
    }

    [Fact]
    public void A_competency_section_says_so_explicitly_when_no_scores_exist_yet()
    {
        var @case = Case.Start(CaseBotProjectId.New(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var template = new ReportTemplate(Guid.NewGuid(), "Standard Report");
        template.AddSection("Yetkinlik Özeti", order: 0);

        var report = @case.GenerateReport(template, DateTimeOffset.UtcNow);

        report.Sections.Single().Content.Should().Contain("No competency scores available");
    }
}
