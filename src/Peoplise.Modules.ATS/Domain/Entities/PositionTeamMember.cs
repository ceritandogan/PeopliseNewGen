using Peoplise.Modules.ATS.Domain.ValueObjects;
using Peoplise.SharedKernel.Domain;

namespace Peoplise.Modules.ATS.Domain.Entities;

/// <summary>A user's role-based access on one <c>Position</c>'s hiring team.</summary>
public sealed class PositionTeamMember : BaseEntity<Guid>
{
    public string UserId { get; private set; } = string.Empty;
    public PositionRole Role { get; private set; }

    private PositionTeamMember()
    {
        // Reserved for EF Core materialization.
    }

    public PositionTeamMember(Guid id, string userId, PositionRole role) : base(id)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("A team member must have a user id.", nameof(userId));

        UserId = userId;
        Role = role;
    }
}
