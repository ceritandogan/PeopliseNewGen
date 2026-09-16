using Peoplise.SharedKernel.Auditing;
using Peoplise.SharedKernel.MultiTenancy;

namespace Peoplise.Infrastructure.Identity;

/// <summary>
/// The minimal credential store OpenIddict's password grant validates against.
/// Deliberately not a SharedKernel <c>AggregateRoot</c> or a business-module concept:
/// this is infrastructure-level login plumbing (one row per person who can sign in),
/// not a domain aggregate with its own invariants/events — those would only be
/// warranted by a real user-management feature (invite flows, role administration,
/// multi-tenant membership), none of which are in scope yet. One tenant per user for
/// now; a person belonging to more than one tenant isn't supported.
/// </summary>
public sealed class User : IHasTenant, IAuditableEntity
{
    private readonly List<string> _roles = [];

    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public IReadOnlyCollection<string> Roles => _roles.AsReadOnly();

    public Guid TenantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }

    private User()
    {
        // Reserved for EF Core materialization.
    }

    public User(Guid id, Guid tenantId, string email, string passwordHash, string displayName, IEnumerable<string> roles)
    {
        Id = id;
        TenantId = tenantId;
        Email = email;
        PasswordHash = passwordHash;
        DisplayName = displayName;
        _roles.AddRange(roles);
    }

    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;
}
