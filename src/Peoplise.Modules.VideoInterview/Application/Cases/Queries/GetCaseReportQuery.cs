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
/// Read-through: returns the case's most recent generated report, generating (and
/// persisting) one on the project's template if none exists yet. A pragmatic relaxation
/// of strict query/command separation — report generation is cheap, deterministic, and
/// has no meaningful "who asked for this" semantics worth a separate command for.
/// </summary>
public sealed class GetCaseReportQueryHandler : IRequestHandler<GetCaseReportQuery, Result<CaseReportDto>>
{
    private readonly AppDbContext _context;
    private readonly IRepository<CaseBotProject, CaseBotProjectId> _projects;
    private readonly IUnitOfWork _unitOfWork;

    public GetCaseReportQueryHandler(
        AppDbContext context,
        IRepository<CaseBotProject, CaseBotProjectId> projects,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _projects = projects;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CaseReportDto>> Handle(GetCaseReportQuery request, CancellationToken cancellationToken)
    {
        var caseId = CaseId.From(request.CaseId);

        var @case = await _context.Set<Case>()
            .Include(c => c.Reports)
            .SingleOrDefaultAsync(c => c.Id == caseId, cancellationToken);

        if (@case is null)
            return Result.Failure<CaseReportDto>(Error.NotFound("Case.NotFound", $"No case '{request.CaseId}' was found."));

        var existingReport = @case.Reports.OrderByDescending(r => r.GeneratedAt).FirstOrDefault();
        if (existingReport is not null)
            return Result.Success(ToDto(request.CaseId, existingReport.GeneratedAt, existingReport.Sections));

        var project = await _projects.GetByIdAsync(@case.CaseBotProjectId, cancellationToken);
        if (project is null)
            return Result.Failure<CaseReportDto>(Error.NotFound("CaseBotProject.NotFound", "The case's project could not be found."));

        var report = @case.GenerateReport(project.ReportTemplate, DateTimeOffset.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ToDto(request.CaseId, report.GeneratedAt, report.Sections));
    }

    private static CaseReportDto ToDto(Guid caseId, DateTimeOffset generatedAt, IEnumerable<Domain.Entities.ReportSectionContent> sections) =>
        new(caseId, generatedAt, sections.Select(s => new ReportSectionDto(s.Title, s.Content)).ToList());
}
