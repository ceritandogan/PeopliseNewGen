# Candidate app auth is a separate, narrowly-scoped resource-access token

The candidate app has no login screen and no candidate account to log into — a candidate reaches a conversation or case purely by holding a URL. Today that URL has no real credential in it at all: `POST /api/conversations/{id}/responses` and `POST /api/cases/{id}/video-answers` are `[AllowAnonymous]` and trust a bare GUID, so anyone who observes or guesses that GUID can act as the candidate. We're closing this with a signed, stateless token minted the instant a conversation/case starts, bound to exactly that resource, and required on every subsequent write to it.

This is deliberately a **third** token system in the codebase, next to OpenIddict's bearer tokens (panel API calls) and its login-page cookie (Stage 11). It is not an OpenIddict grant, not a JWT, and not reused across the two existing systems.

## Considered Options

- **Model the candidate as an OpenIddict client/grant** (e.g., a public client issuing a scoped access token) — rejected: OpenIddict's whole model assumes an authenticatable identity (a `User` row, a login step) that candidates don't have and this feature isn't adding. Bending the authorization-server model to fit "a bearer capability for one resource, no identity behind it" would be more machinery than the problem needs.
- **A full JWT** (via the `Microsoft.IdentityModel.Tokens`/`JsonWebTokenHandler` already pulled in transitively by OpenIddict) — rejected: JWT's issuer/audience/claims-schema machinery solves problems this token doesn't have (no third-party verifiers, no interop requirement). A small hand-rolled `base64url(payload) + "." + HMAC-SHA256` format is enough, and matches this codebase's already-demonstrated preference for hand-rolled auth plumbing over a library for a narrow need (see ADR 0002's PKCE decision).
- **An opaque, server-stored token** (a new table, looked up per request) — rejected: no requirement here for instant revocation (the underlying conversation/case can already be anonymized via the existing KVKK withdraw-consent path, which makes any token pointing at it harmless), so the extra table and per-request lookup buys nothing a signed, self-verifying token doesn't already give for free.

## Consequences

- A third signing key to manage in config (`Auth:CandidateLinks:SigningKey`), alongside OpenIddict's certificates — deliberately separate, so a compromise or rotation of one system's key says nothing about the other's.
- The token happens to survive a page refresh, since it lives in the URL rather than in-memory React state — a welcome side effect, but resuming a conversation with its full history hydrated is explicitly not built here; that's a separate feature (the token only proves who's allowed to act, not what UI shows what).
- Real link delivery (email/SMS) remains unbuilt — the token exists and is enforced, but nothing sends it anywhere yet. The candidate app shows/copies the link within the current session, same as today's UX.
