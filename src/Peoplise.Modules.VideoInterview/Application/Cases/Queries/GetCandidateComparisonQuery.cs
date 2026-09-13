using MediatR;
using Microsoft.EntityFrameworkCore;
using Peoplise.Infrastructure.Persistence;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.VideoInterview.Application.Cases.Queries;

/// <summary>"Aday karşılaştırma: Aynı pozisyondaki adayların yetkinlik sıralaması" — every completed case under a project, ranked by overall score.</summary>
public sealed record GetCandidateComparisonQuery(Guid CaseBotProjectId) : IRequest<Result<IReadOnlyList<CandidateComparisonItem>>>;

public sealed record CandidateComparisonItem(
    Guid CaseId,
    Guid CandidateId,
    decimal OverallScore,
    IReadOnlyDictionary<Guid, decimal> CompetencyScores);

public sealed class GetCandidateComparisonQueryHandler
    : IRequestHandler<GetCandidateComparisonQuery, Result<IReadOnlyList<CandidateComparisonItem>>>
{
    private readonly AppDbContext _context;

    public GetCandidateComparisonQueryHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IReadOnlyList<CandidateComparisonItem>>> Handle(
        GetCandidateComparisonQuery request, CancellationToken cancellationToken)
    {
        var projectId = CaseBotProjectId.From(request.CaseBotProjectId);

        var completedCases = await _context.Set<Case>()
            .Include(c => c.AssessmentResult)
            .Where(c => c.CaseBotProjectId == projectId && c.Status == CaseStatus.Completed)
            .ToListAsync(cancellationToken);

        var items = completedCases
            .Where(c => c.AssessmentResult is not null && c.AssessmentResult.CompetencyResults.Count > 0)
            .Select(c =>
            {
                var scores = c.AssessmentResult!.CompetencyResults.ToDictionary(r => r.CompetencyId, r => r.Score);
                var overall = Math.Round(scores.Values.Average(), 2);
                return new CandidateComparisonItem(c.Id.Value, c.CandidateId, overall, scores);
            })
            .OrderByDescending(item => item.OverallScore)
            .ToList();

        return Result.Success<IReadOnlyList<CandidateComparisonItem>>(items);
    }
}
