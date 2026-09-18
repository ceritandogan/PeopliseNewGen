using MediatR;
using Microsoft.EntityFrameworkCore;
using Peoplise.Infrastructure.Persistence;
using Peoplise.Modules.VideoInterview.Domain.Aggregates;
using Peoplise.Modules.VideoInterview.Domain.ValueObjects;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.VideoInterview.Application.Cases.Queries;

/// <summary>
/// Not a business query — plumbing for the anonymous candidate-facing case endpoints (no
/// session, so no tenant claim to resolve from). Mirrors HrBot's
/// <c>ResolveConversationTenantQuery</c>: finds which tenant a case belongs to *before*
/// dispatching <c>SubmitVideoAnswerCommand</c>, so the API layer can set that as the
/// request's tenant override. Cross-tenant by design (<c>IgnoreQueryFilters</c>) — the
/// case id is the caller's only credential here.
/// </summary>
public sealed record ResolveCaseTenantQuery(Guid CaseId) : IRequest<Result<Guid>>;

public sealed class ResolveCaseTenantQueryHandler : IRequestHandler<ResolveCaseTenantQuery, Result<Guid>>
{
    private readonly AppDbContext _context;

    public ResolveCaseTenantQueryHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(ResolveCaseTenantQuery request, CancellationToken cancellationToken)
    {
        var caseId = CaseId.From(request.CaseId);

        var tenantId = await _context.Set<Case>()
            .IgnoreQueryFilters()
            .Where(c => c.Id == caseId)
            .Select(c => (Guid?)c.TenantId)
            .SingleOrDefaultAsync(cancellationToken);

        return tenantId is null
            ? Result.Failure<Guid>(Error.NotFound("Case.NotFound", $"No case '{request.CaseId}' was found."))
            : Result.Success(tenantId.Value);
    }
}
