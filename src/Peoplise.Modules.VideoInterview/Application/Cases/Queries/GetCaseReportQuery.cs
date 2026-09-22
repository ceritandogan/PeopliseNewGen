using MediatR;
using Microsoft.EntityFrameworkCore;
using Peoplise.Infrastructure.Persistence;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.Persistence;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.VideoInterview.Application.Cases.Queries;

public sealed record GetCaseReportQuery(Guid CaseId) : IRequest<Result<CaseReportDto>>;

public sealed record CaseReportDto(Guid CaseId, DateTimeOffset GeneratedAt, IReadOnlyList<ReportSectionDto> Sections);

public sealed record ReportSectionDto(string Title, string Content);

/// <summary>
/// Recomputes the report fresh on every call against the case's current state — never
/// reuses or persists a previous <see cref="Domain.Entities.Report"/>. See ADR 0005:
/// generation is cheap/deterministic, and a cached report can silently go stale relative
/// to scoring that happens after it was first viewed.
/// </summary>
public sealed class GetCaseReportQueryHandler : IRequestHandler<GetCaseReportQuery, Result<CaseReportDto>>
{
    private readonly AppDbContext _context;
    private readonly IRepository<CaseBotProject, CaseBotProjectId> _projects;

    public GetCaseReportQueryHandler(AppDbContext context, IRepository<CaseBotProject, CaseBotProjectId> projects)
    {
        _context = context;
        _projects = projects;
    }

    public async Task<Result<CaseReportDto>> Handle(GetCaseReportQuery request, CancellationToken cancellationToken)
    {
        var caseId = CaseId.From(request.CaseId);

        var @case = await _context.Set<Case>().SingleOrDefaultAsync(c => c.Id == caseId, cancellationToken);
        if (@case is null)
            return Result.Failure<CaseReportDto>(Error.NotFound("Case.NotFound", $"No case '{request.CaseId}' was found."));

        if (@case.Status != CaseStatus.Completed)
        {
            return Result.Failure<CaseReportDto>(Error.Conflict(
                "Case.NotCompleted", "A report is only available once this case has completed."));
        }

        var project = await _projects.GetByIdAsync(@case.CaseBotProjectId, cancellationToken);
        if (project is null)
            return Result.Failure<CaseReportDto>(Error.NotFound("CaseBotProject.NotFound", "The case's project could not be found."));

        var report = @case.GenerateReport(project.ReportTemplate, DateTimeOffset.UtcNow);

        return Result.Success(ToDto(request.CaseId, report.GeneratedAt, report.Sections));
    }

    private static CaseReportDto ToDto(Guid caseId, DateTimeOffset generatedAt, IEnumerable<Domain.Entities.ReportSectionContent> sections) =>
        new(caseId, generatedAt, sections.Select(s => new ReportSectionDto(s.Title, s.Content)).ToList());
}
