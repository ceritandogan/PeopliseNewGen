# Panel login moves to redirect-based Authorization Code + PKCE, ROPC removed

The panel app's login used OpenIddict's password (ROPC) grant — the fastest way to a working login in Stage 1, but deprecated in OAuth 2.1 precisely because it requires a public client (a browser SPA, unable to hold a secret) to handle the user's password directly. We're replacing it with the standard shape for a public client: the browser navigates to a server-hosted `/connect/authorize` login page (backed by a cookie authentication scheme, not the React SPA), the user authenticates there, and the SPA completes a PKCE-secured Authorization Code exchange after the redirect back. ROPC is removed once the new flow works, rather than kept alongside it.

## Considered Options

- **Backend-for-Frontend (server-side session, httpOnly cookie, tokens never reach JS)** — rejected for this pass: closes the localStorage/XSS exposure more thoroughly, but is a full frontend auth-model rewrite (no more Zustand token store, cookie-based CSRF protection needed), a substantially bigger scope than "harden the existing flow." Left as a future option if that exposure needs closing later.
- **An OIDC client library (e.g. `oidc-client-ts`) for the frontend's PKCE/redirect handling** — rejected: PKCE itself is a small, well-documented amount of crypto (`crypto.getRandomValues`, SHA-256, base64url) and the codebase already hand-rolls its token-exchange/refresh plumbing (`tokenEndpoint.ts`, `authStore.ts`) rather than using a library; extending that pattern keeps the dependency surface as small as it already is.
- **Keeping ROPC alongside Authorization Code** (e.g. for scripted/CI login) — rejected: doubles the attack surface and the code path to maintain for no real benefit once the real flow exists. Test/verification access instead uses a real browser-driven flow for live checks and a `TestAuthHandler` (standard ASP.NET Core test pattern) for automated tests, neither of which needs ROPC.

## Consequences

- `/connect/authorize` needs its own cookie authentication scheme, distinct from the bearer-token scheme the rest of the API uses — a second auth mechanism to reason about, scoped tightly to the login page itself.
- The panel's `LoginPage.tsx` stops being a credential form; the actual login UI is a minimal, unstyled server-rendered page for this pass. Restyling it to match the panel's look is a cheap, separate follow-up, not bundled here.
- This is the first integration-test project in the solution (`Peoplise.Api.Tests`, `WebApplicationFactory`-based) — a real, if overdue, structural addition, not scope creep specific to auth.
