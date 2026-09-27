# Workspace signup: `Tenant` as a plain entity, session issued only via the existing OAuth flow

Until now there was no `Tenant` entity at all — just a `TenantId` value object stamped on rows, and one hardcoded demo tenant/user pair inserted by `DatabaseSeeder`. There was no way for anyone to create a new workspace. We're adding self-service signup (`POST /api/tenants`, public): a workspace name + a first user's email/password, creating both rows atomically. A user still belongs to exactly one tenant forever — multi-tenant membership/switching is out of scope here.

Two decisions are worth recording because a future reader would reasonably assume the opposite:

**`Tenant` is a plain class in `Peoplise.Infrastructure`, not a `SharedKernel.AggregateRoot`.** Every other feature in this codebase that introduces a new concept gives it a full module (domain aggregate, repository, MediatR command/handler, controller) — see `CaseBotProject`/`Competency`. `Tenant` deliberately follows `User`'s pattern instead: no repository, no domain events, queried directly via `AppDbContext.Tenants`. It has zero business behavior today (no rename, suspend, plan/billing) — an aggregate with nothing to protect and nothing to raise events about would be pure ceremony. `TenantsController` itself skips MediatR for the same reason, mirroring `AuthorizationController`'s own directness.

**Signup does not mint a session.** `POST /api/tenants` returns only the new tenant id and slug; the frontend then calls the exact same `beginLogin()` OAuth Authorization Code + PKCE redirect every sign-in uses, so the user re-enters the password they just chose on OpenIddict's own `/connect/login` page. The alternative — having signup mint tokens directly — would mean either bypassing OpenIddict's issuance path or faking an internal `/connect/token` call, creating a second way to produce a valid session. One extra password entry is worth not doing that.

## Considered Options

- **Full `Tenant` aggregate + its own module** — rejected for now: no invariant or behavior exists yet to justify a repository and command/handler layer for two fields (name, slug). If tenants ever grow real lifecycle (suspension, plan changes), promoting it later is a small, contained refactor — the entity shape (plain class, explicit `IEntityTypeConfiguration`) is already exactly `User`'s, so there's precedent either way.
- **Session minted directly by the signup endpoint** — rejected: reuses none of the already-tested OpenIddict issuance path, and introduces a second, parallel way to obtain a valid access token — a real increase in auth surface for a minor UX gain.
- **Tenant-scoped login (email unique per tenant, not globally)** — rejected: `AuthorizationController.LoginSubmit` already does a tenant-unscoped email lookup (`Users.Email` has long had a unique index); making login tenant-aware would be a separate, larger redesign of the login flow, not something this feature needs to force.

## Consequences

- `Users.Email`'s pre-existing global unique index is now load-bearing for signup too: a duplicate email is rejected at signup with a clear `Tenant.EmailAlreadyRegistered` error rather than surfacing as a confusing constraint violation.
- `DatabaseSeeder` now seeds a matching `Tenant` row (`"Demo Workspace"`, slug `demo`) for the hardcoded demo tenant, and its idempotency check was widened to look at both `Tenants` and `Users` — the same "checked only one of two things that must be seeded together" bug class fixed once already for `CaseBotProject`/`CaseFlows`.
- The panel header's "Çalışma alanı seç" dropdown is now a plain read-only label showing the real workspace name (via `GET /api/tenants/current`) instead of a `<select>` that implied switching between workspaces that was never possible.
