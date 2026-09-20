/**
 * PKCE (RFC 7636), hand-rolled rather than pulled from an OIDC client library — see
 * ADR 0002: this is the small, well-documented amount of crypto the redirect-based
 * Authorization Code flow needs, extending the same hand-rolled-plumbing pattern this
 * package already uses for token exchange and refresh (`tokenEndpoint.ts`).
 */

function base64UrlEncode(bytes: Uint8Array): string {
  let binary = "";
  for (const byte of bytes) binary += String.fromCharCode(byte);
  return btoa(binary).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}

/** A high-entropy, unguessable secret the browser holds across the redirect and proves it still holds on the way back. */
export function generateCodeVerifier(): string {
  const bytes = new Uint8Array(32);
  crypto.getRandomValues(bytes);
  return base64UrlEncode(bytes);
}

/** SHA-256 + base64url of the verifier — sent up front, so the token exchange can prove possession of the verifier without ever transmitting it over the redirect. */
export async function deriveCodeChallenge(verifier: string): Promise<string> {
  const digest = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(verifier));
  return base64UrlEncode(new Uint8Array(digest));
}

/** CSRF protection for the redirect itself, independent of PKCE — proves the callback corresponds to a login *this* browser tab actually initiated. */
export function generateState(): string {
  const bytes = new Uint8Array(16);
  crypto.getRandomValues(bytes);
  return base64UrlEncode(bytes);
}
