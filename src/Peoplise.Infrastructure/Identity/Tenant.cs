using Peoplise.SharedKernel.Auditing;

namespace Peoplise.Infrastructure.Identity;

/// <summary>
/// One customer organization ("workspace" in the panel's UI copy). Deliberately a plain
/// class, not a SharedKernel <c>AggregateRoot</c> — same reasoning as <see cref="User"/>:
/// it has no business behavior yet (no rename, suspend, plan/billing), just an identity
/// and a display name, so there's nothing for domain methods or a repository to do.
/// Does NOT implement <see cref="Peoplise.SharedKernel.MultiTenancy.IHasTenant"/> — a
/// tenant doesn't belong to a tenant.
/// </summary>
public sealed class Tenant : IAuditableEntity
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }

    private Tenant()
    {
        // Reserved for EF Core materialization.
    }

    public Tenant(Guid id, string name, string slug)
    {
        Id = id;
        Name = name;
        Slug = slug;
    }
}
