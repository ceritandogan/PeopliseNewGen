using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Peoplise.Infrastructure.Identity;
using Peoplise.Infrastructure.Persistence;
using Peoplise.SharedKernel.MultiTenancy;
using Peoplise.SharedKernel.Results;

namespace Peoplise.Api.Controllers;

/// <summary>
/// Workspace (tenant) self-service signup, plus a way for an already-authenticated user
/// to see their own workspace's name. Deliberately not routed through MediatR/a module —
/// <see cref="Tenant"/> and <see cref="User"/> are both plain Infrastructure entities
/// with no domain behavior yet, so this follows <see cref="AuthorizationController"/>'s
/// own directness (talks to <see cref="AppDbContext"/> directly) rather than inventing a
/// module + repository + command handler for two rows with no invariants beyond
/// uniqueness. See ADR 0007.
/// </summary>
[ApiController]
[Route("api/tenants")]
public sealed class TenantsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public TenantsController(AppDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public sealed record CreateTenantRequest(string WorkspaceName, string DisplayName, string Email, string Password);

    public sealed record CreateTenantResponse(Guid TenantId, string Slug);

    /// <summary>
    /// The one deliberately anonymous write in this controller — there is no session yet
    /// by definition, since this is what creates the very first user of a brand-new
    /// tenant. Mints the new tenant id up front, then does both inserts inside one
    /// <see cref="AmbientTenantOverride"/> scope (same pattern as
    /// <see cref="DatabaseSeeder"/> and the candidate "apply" endpoint) since
    /// <c>TenantInterceptor</c> refuses to save any <c>IHasTenant</c> row (the new
    /// <see cref="User"/>) with no tenant resolved.
    /// </summary>
    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Create(CreateTenantRequest request, CancellationToken cancellationToken)
    {
        var validation = Validate(request);
        if (validation is not null)
            return validation.ToActionResult(this);

        // Global lookup, matching AuthorizationController.LoginSubmit's own
        // IgnoreQueryFilters email lookup — login isn't scoped by tenant, so a duplicate
        // email across two tenants would make login ambiguous (SingleOrDefaultAsync would
        // throw). Reject here instead, before that can ever happen.
        var emailTaken = await _context.Users.IgnoreQueryFilters()
            .AnyAsync(u => u.Email == request.Email && !u.IsDeleted, cancellationToken);
        if (emailTaken)
        {
            return Result.Failure<CreateTenantResponse>(
                Error.Conflict("Tenant.EmailAlreadyRegistered", "An account with this email already exists.")).ToActionResult(this);
        }

        var slug = await GenerateUniqueSlugAsync(request.WorkspaceName, cancellationToken);
        var tenantId = Guid.NewGuid();

        var tenant = new Tenant(id: tenantId, name: request.WorkspaceName.Trim(), slug: slug);

        var hasher = new PasswordHasher<User>();
        var user = new User(
            id: Guid.NewGuid(),
            tenantId: tenantId,
            email: request.Email.Trim(),
            passwordHash: string.Empty,
            displayName: request.DisplayName.Trim(),
            roles: []);
        user.SetPasswordHash(hasher.HashPassword(user, request.Password));

        _context.Tenants.Add(tenant);
        _context.Users.Add(user);

        using (AmbientTenantOverride.Begin(TenantId.From(tenantId)))
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Ok(new CreateTenantResponse(tenantId, slug));
    }

    public sealed record CurrentTenantResponse(string Name, string Slug);

    [Authorize]
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId is null)
        {
            return Result.Failure<CurrentTenantResponse>(
                Error.Failure("Tenant.NotResolved", "No tenant could be resolved for this request.")).ToActionResult(this);
        }

        var tenant = await _context.Tenants.SingleOrDefaultAsync(t => t.Id == tenantId.Value, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure<CurrentTenantResponse>(
                Error.NotFound("Tenant.NotFound", "The current tenant could not be found.")).ToActionResult(this);
        }

        return Ok(new CurrentTenantResponse(tenant.Name, tenant.Slug));
    }

    private static Result<CreateTenantResponse>? Validate(CreateTenantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.WorkspaceName))
            return Result.Failure<CreateTenantResponse>(Error.Validation("Tenant.WorkspaceNameRequired", "Workspace name is required."));

        if (string.IsNullOrWhiteSpace(request.DisplayName))
            return Result.Failure<CreateTenantResponse>(Error.Validation("Tenant.DisplayNameRequired", "Your name is required."));

        if (string.IsNullOrWhiteSpace(request.Email) || !EmailPattern.IsMatch(request.Email))
            return Result.Failure<CreateTenantResponse>(Error.Validation("Tenant.InvalidEmail", "A valid email address is required."));

        if (string.IsNullOrEmpty(request.Password) || request.Password.Length < 8)
            return Result.Failure<CreateTenantResponse>(Error.Validation("Tenant.PasswordTooShort", "Password must be at least 8 characters."));

        return null;
    }

    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    private async Task<string> GenerateUniqueSlugAsync(string workspaceName, CancellationToken cancellationToken)
    {
        var baseSlug = Slugify(workspaceName);
        var slug = baseSlug;
        var suffix = 2;

        while (await _context.Tenants.AnyAsync(t => t.Slug == slug, cancellationToken))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix++;
        }

        return slug;
    }

    private static string Slugify(string value)
    {
        var lowered = value.Trim().ToLowerInvariant();
        var hyphenated = Regex.Replace(lowered, @"[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrEmpty(hyphenated) ? "workspace" : hyphenated;
    }
}
