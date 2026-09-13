import axios from "axios";
import type { AuthSession } from "@peoplise/ui";

/**
 * Talks to OpenIddict's `/connect/token` endpoint directly, on a plain axios instance —
 * deliberately *not* the shared `httpClient` from `./http`, so this never recurses back
 * into `httpClient`'s own 401-triggered refresh interceptor.
 *
 * TODO(auth): the password grant matches the backend's current ROPC setup (see
 * `Peoplise.Infrastructure.DependencyInjection.AddAuth`'s TODO) — migrate this to
 * Authorization Code + PKCE together with that backend change.
 */
const tokenClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000",
});

interface TokenResponse {
  access_token: string;
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
 * The backend's token endpoint doesn't return user profile info alongside the token
 * (OpenIddict's password grant response is token-only) — decode it from the access
 * token's own claims instead of a second round-trip.
 */
function decodeUserFromAccessToken(accessToken: string): AuthSession["user"] {
  const [, payloadSegment] = accessToken.split(".");
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

export async function passwordGrant(email: string, password: string): Promise<AuthSession> {
  const token = await requestToken({
    grant_type: "password",
    username: email,
    password,
    scope: "openid offline_access",
  });
  return toSession(token, decodeUserFromAccessToken(token.access_token));
}

export async function refreshTokenGrant(refreshToken: string): Promise<AuthSession> {
  const token = await requestToken({ grant_type: "refresh_token", refresh_token: refreshToken });
  return toSession(token, decodeUserFromAccessToken(token.access_token));
}
