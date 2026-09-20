import axios from "axios";
import type { AuthSession } from "@peoplise/ui";
import { deriveCodeChallenge, generateCodeVerifier, generateState } from "./pkce";

/**
 * Talks to OpenIddict's `/connect/token` (and `/connect/authorize`) endpoints directly,
 * on a plain axios instance — deliberately *not* the shared `httpClient` from `./http`,
 * so this never recurses back into `httpClient`'s own 401-triggered refresh interceptor.
 *
 * Authorization Code + PKCE, redirect-based — see ADR 0002 for why this replaced the
 * password grant this endpoint used to call directly.
 */
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000";
const CLIENT_ID = import.meta.env.VITE_OAUTH_CLIENT_ID ?? "peoplise-panel";
const REDIRECT_URI = () => `${window.location.origin}/auth/callback`;

const tokenClient = axios.create({ baseURL: API_BASE_URL });

interface TokenResponse {
  access_token: string;
  id_token: string;
  refresh_token: string;
  expires_in: number;
  token_type: string;
}

async function requestToken(body: Record<string, string>): Promise<TokenResponse> {
  const { data } = await tokenClient.post<TokenResponse>(
    "/connect/token",
    new URLSearchParams(body),
    { headers: { "Content-Type": "application/x-www-form-urlencoded" } },
  );
  return data;
}

function toSession(token: TokenResponse, user: AuthSession["user"]): AuthSession {
  return {
    accessToken: token.access_token,
    refreshToken: token.refresh_token,
    expiresAt: Date.now() + token.expires_in * 1000,
    user,
  };
}

/**
 * Reads user info from the **id_token**, not the access_token: OpenIddict encrypts
 * access tokens by default (opaque, only this API can read them — that's deliberate,
 * not a bug), but issues a signed-only id_token whenever "openid" scope is requested,
 * exactly so the client has something decodable. The backend's `AuthorizationController`
 * puts sub/email/name/role into both tokens; `tenant_id` is access-token-only since only
 * the API needs it.
 */
function decodeUserFromIdToken(idToken: string): AuthSession["user"] {
  const [, payloadSegment] = idToken.split(".");
  if (!payloadSegment) return { id: "", email: "", roles: [] };

  try {
    const payload = JSON.parse(atob(payloadSegment.replace(/-/g, "+").replace(/_/g, "/")));
    return {
      id: payload.sub ?? "",
      email: payload.email ?? "",
      roles: Array.isArray(payload.role) ? payload.role : payload.role ? [payload.role] : [],
    };
  } catch {
    return { id: "", email: "", roles: [] };
  }
}

const VERIFIER_KEY = "peoplise.auth.pkce_verifier";
const STATE_KEY = "peoplise.auth.pkce_state";
const RETURN_TO_KEY = "peoplise.auth.return_to";

/**
 * Kicks off the redirect: stashes the PKCE verifier, CSRF state, and where to send the
 * user back to in sessionStorage (survives the hard navigation away and back, unlike
 * React Router state), then navigates the whole page to `/connect/authorize` — this
 * never resolves in practice, since the browser leaves this app before the returned
 * promise would settle.
 */
export async function beginLogin(returnTo: string): Promise<void> {
  const verifier = generateCodeVerifier();
  const challenge = await deriveCodeChallenge(verifier);
  const state = generateState();

  sessionStorage.setItem(VERIFIER_KEY, verifier);
  sessionStorage.setItem(STATE_KEY, state);
  sessionStorage.setItem(RETURN_TO_KEY, returnTo);

  const authorizeUrl = new URL("/connect/authorize", API_BASE_URL);
  authorizeUrl.searchParams.set("client_id", CLIENT_ID);
  authorizeUrl.searchParams.set("redirect_uri", REDIRECT_URI());
  authorizeUrl.searchParams.set("response_type", "code");
  authorizeUrl.searchParams.set("scope", "openid offline_access email profile");
  authorizeUrl.searchParams.set("code_challenge", challenge);
  authorizeUrl.searchParams.set("code_challenge_method", "S256");
  authorizeUrl.searchParams.set("state", state);

  window.location.assign(authorizeUrl.toString());
}

export interface CompletedLogin {
  session: AuthSession;
  returnTo: string;
}

/**
 * Called from the `/auth/callback` page once the browser lands back with `?code=&state=`.
 * Validates the `state` against what `beginLogin` stashed (rejecting a forged or replayed
 * callback) before ever exchanging the code, then clears the stashed PKCE state either way
 * — a callback is one-shot, successful or not.
 */
export async function completeLogin(params: URLSearchParams): Promise<CompletedLogin> {
  const verifier = sessionStorage.getItem(VERIFIER_KEY);
  const expectedState = sessionStorage.getItem(STATE_KEY);
  const returnTo = sessionStorage.getItem(RETURN_TO_KEY) ?? "/";

  sessionStorage.removeItem(VERIFIER_KEY);
  sessionStorage.removeItem(STATE_KEY);
  sessionStorage.removeItem(RETURN_TO_KEY);

  const error = params.get("error");
  if (error) throw new Error(params.get("error_description") ?? error);

  const code = params.get("code");
  const state = params.get("state");
  if (!code || !verifier || !state || state !== expectedState) {
    throw new Error("This login attempt is invalid or has expired. Please sign in again.");
  }

  const token = await requestToken({
    grant_type: "authorization_code",
    code,
    redirect_uri: REDIRECT_URI(),
    code_verifier: verifier,
    client_id: CLIENT_ID,
  });

  return { session: toSession(token, decodeUserFromIdToken(token.id_token)), returnTo };
}

export async function refreshTokenGrant(refreshToken: string): Promise<AuthSession> {
  // client_id is mandatory here too, same as the code exchange above — a public client
  // has no secret to authenticate with, so OpenIddict requires client_id on every
  // /connect/token call to know which client is asking (caught live: this call 400'd
  // with "the mandatory 'client_id' parameter is missing" until this was added).
  const token = await requestToken({ grant_type: "refresh_token", refresh_token: refreshToken, client_id: CLIENT_ID });
  return toSession(token, decodeUserFromIdToken(token.id_token));
}

/**
 * Best-effort: ends the server-side login-page cookie session so a later
 * /connect/authorize visit on this browser requires signing in again. Failure here isn't
 * fatal to "logging out" from this app's point of view — clearing the local session
 * (which the caller does regardless) is what actually signs the user out of the panel.
 */
export async function logout(): Promise<void> {
  try {
    await tokenClient.post("/connect/logout", null, { withCredentials: true });
  } catch {
    // See doc comment above — swallowed deliberately.
  }
}
