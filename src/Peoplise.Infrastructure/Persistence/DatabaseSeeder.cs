using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Peoplise.Infrastructure.Identity;
using Peoplise.SharedKernel.MultiTenancy;

namespace Peoplise.Infrastructure.Persistence;

/// <summary>
/// Development-only seed data: one demo tenant and one login-capable user, so there's a
/// way into the panel at all before any real user-management/signup flow exists. Not
/// meant to run anywhere but a local dev database — see where this is called from in
/// <c>Program.cs</c>, gated to the Development environment.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>A fixed, obviously-a-placeholder tenant id every seeded demo record belongs to.</summary>
    public static readonly Guid DemoTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public const string DemoUserEmail = "demo@peoplise.local";
    public const string DemoUserPassword = "Demo123!";

    public static async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        // IgnoreQueryFilters: no tenant is resolved during startup seeding (nothing has
        // logged in yet), so the tenant global query filter would otherwise hide
        // everything, including the check for whether seeding already ran.
        var alreadySeeded = await context.Users.IgnoreQueryFilters().AnyAsync(cancellationToken);
        if (alreadySeeded) return;

        var hasher = new PasswordHasher<User>();
        var user = new User(
            id: Guid.NewGuid(),
            tenantId: DemoTenantId,
            email: DemoUserEmail,
            passwordHash: string.Empty,
            displayName: "Demo User",
            roles: ["Admin"]);
        user.SetPasswordHash(hasher.HashPassword(user, DemoUserPassword));

        context.Users.Add(user);

        // No request (and so no tenant JWT claim) exists yet at startup — same
        // ambient-override mechanism the anonymous apply endpoint uses.
        using var _ = AmbientTenantOverride.Begin(TenantId.From(DemoTenantId));
        await context.SaveChangesAsync(cancellationToken);
    }
}
