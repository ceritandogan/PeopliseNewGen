using MediatR;
using Microsoft.EntityFrameworkCore;
using Peoplise.Infrastructure.Persistence;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.ATS.Application.Positions.Queries;

/// <summary>
/// Not a business query — plumbing for the one anonymous write this module accepts
/// (a candidate applying, with no session and so no tenant claim to resolve from). The
/// API layer uses this to find which tenant the target position belongs to *before*
/// dispatching <c>SubmitCandidateApplicationCommand</c>, then sets that as the request's
/// tenant override so the write lands correctly. Cross-tenant by design
/// (<c>IgnoreQueryFilters</c>) — the position id is the caller's only credential here,
/// same trust model as a share link.
/// </summary>
public sealed record ResolvePositionTenantQuery(Guid PositionId) : IRequest<Result<Guid>>;

public sealed class ResolvePositionTenantQueryHandler : IRequestHandler<ResolvePositionTenantQuery, Result<Guid>>
{
    private readonly AppDbContext _context;

    public ResolvePositionTenantQueryHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(ResolvePositionTenantQuery request, CancellationToken cancellationToken)
    {
        var positionId = PositionId.From(request.PositionId);

        var tenantId = await _context.Set<Position>()
            .IgnoreQueryFilters()
            .Where(p => p.Id == positionId)
            .Select(p => (Guid?)p.TenantId)
            .SingleOrDefaultAsync(cancellationToken);

        return tenantId is null
            ? Result.Failure<Guid>(Error.NotFound("Position.NotFound", $"No position '{request.PositionId}' was found."))
            : Result.Success(tenantId.Value);
    }
}
