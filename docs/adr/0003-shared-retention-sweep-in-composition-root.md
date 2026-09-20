# The KVKK retention sweep moves to the composition root, covering every module

ADR 0001 put `RetentionExpiryBackgroundService` inside the VideoInterview module because, at the time, VideoInterview's `Case` was the only aggregate with consent/retention machinery. Now HrBot's `Conversation` needs the identical treatment (KVKK applies to it just as much), and a single 24h sweep covering both is simpler than two near-identical timer loops. Since modules don't reference each other, a job that needs to know about both `Case`/`CaseBotProject` and `Conversation`/`BotProject` can't legitimately live inside either one — it moves to `Peoplise.Api`, the one place already composing every module together.

## Considered Options

- **A second, HrBot-scoped copy of the background service** — rejected: duplicates the sweep-loop and cross-tenant-query shape for no benefit; two jobs on the same 24h cadence sweeping conceptually the same KVKK obligation is just as much code to maintain as one, with more places for the pattern to drift.
- **Leaving it in VideoInterview and having it reach into HrBot's repositories directly** — rejected outright: violates the module-boundary rule already established everywhere else in this codebase (modules depend on Infrastructure, never on each other).

## Consequences

- Any *third* module that later needs the same KVKK treatment extends this same composition-root service rather than creating its own — the pattern is now "one shared sweep," not "one sweep per module."
- The service's eligibility logic per aggregate type (`CaseRetentionEligibility`, `ConversationRetentionEligibility`) still lives in each module's own Application layer — only the scheduling/orchestration shell moved, not the domain-specific rules.
