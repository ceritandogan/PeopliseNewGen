using MediatR;
using Microsoft.EntityFrameworkCore;
using Peoplise.Infrastructure.Persistence;
using Peoplise.Modules.ATS.Domain.Aggregates;
using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Modules.ATS.Application.Positions.Queries;

/// <summary>
/// Tenant-scoped existence check for an authenticated caller — unlike
/// <see cref="ResolvePositionTenantQuery"/> (which deliberately bypasses the tenant
/// filter for the one anonymous write this module accepts), this relies on the
/// ambient tenant filter already applied to <c>AppDbContext</c> for an authenticated
/// request, so it naturally fails for a position that doesn't exist OR belongs to a
/// different tenant — the caller can't distinguish the two, which is the point.
/// </summary>
public sealed record ValidatePositionExistsQuery(Guid PositionId) : IRequest<Result>;

public sealed class ValidatePositionExistsQueryHandler : IRequestHandler<ValidatePositionExistsQuery, Result>
{
    private readonly AppDbContext _context;

    public ValidatePositionExistsQueryHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(ValidatePositionExistsQuery request, CancellationToken cancellationToken)
    {
        var positionId = PositionId.From(request.PositionId);
        var exists = await _context.Set<Position>().AnyAsync(p => p.Id == positionId, cancellationToken);

        return exists
            ? Result.Success()
            : Result.Failure(Error.NotFound("Position.NotFound", $"No position '{request.PositionId}' was found."));
    }
}
