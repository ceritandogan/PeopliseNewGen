# KVKK retention expiry runs as an in-process, cross-tenant background sweep

We need a scheduled job that anonymizes VideoInterview `Case` data once a tenant's configured retention period elapses. We chose an in-process `BackgroundService` on a 24h `PeriodicTimer` (no new infrastructure, matches the existing `VideoTranscriptionBackgroundService` pattern) that queries `Case` with `IgnoreQueryFilters()` to see every tenant in one pass, then reuses `RequestDataDeletionCommand` per eligible case rather than duplicating its file-deletion/domain/save logic. We also widened `Case.ExpireRetention`'s guard from "case is still open" to "case isn't already anonymized," since the original guard made it impossible to expire retention on the far more common case: a `Completed` interview whose data has simply aged out.

## Considered Options

- **Hangfire/Quartz.NET** instead of an in-process `BackgroundService` — rejected: no job-scheduling library exists in the solution yet, and a 24h cadence doesn't need persistence, retries, or a dashboard.
- **Per-tenant loop** (enumerate tenants, run the sweep once per tenant inside `AmbientTenantOverride`) instead of `IgnoreQueryFilters()` — rejected: retention expiry is a system-level KVKK obligation, not tenant business logic, so a single cross-tenant read is more honest than pretending it's tenant-scoped.
- **Direct repository manipulation in the job** instead of dispatching `RequestDataDeletionCommand` per case — rejected: the command handler already implements the correct file-deletion-before-domain-call ordering; duplicating it risks the two paths drifting apart.

## Consequences

- `IRepository<TAggregateRoot, TId>` gains a generic query method — the first crack in the "id-only" repository abstraction; future call sites needing arbitrary queries should extend this rather than reaching for `DbContext` directly.
- `IgnoreQueryFilters()` is now a precedent for "legitimate system-level cross-tenant reads" — any future use of it outside a background/system context should be treated as suspicious in review.
