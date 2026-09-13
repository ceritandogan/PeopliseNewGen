import { create } from "zustand";

interface TenantState {
  tenantSlug: string | null;
  setTenantSlug: (slug: string) => void;
}

export const useTenantStore = create<TenantState>((set) => ({
  tenantSlug: null,
  setTenantSlug: (tenantSlug) => set({ tenantSlug }),
}));

/**
 * "Tenant Context: URL veya subdomain'den tenant çözümleme." Subdomain wins when both
 * are present — a bookmarked `?tenant=` link shouldn't override the workspace the URL's
 * host already puts you on.
 *
 * Resolution order:
 * 1. Subdomain: `acme.peoplise.app` → `acme` (skips the bare apex domain and `www`).
 * 2. Query param: `?tenant=acme` — used in local dev, where every app runs on
 *    `localhost` and there's no subdomain to read.
 */
export function resolveTenantSlugFromLocation(location: Pick<Location, "hostname" | "search">): string | null {
  const host = location.hostname;
  const parts = host.split(".");

  if (host !== "localhost" && !isIpAddress(host) && parts.length > 2) {
    const subdomain = parts[0];
    if (subdomain !== "www") return subdomain;
  }

  const params = new URLSearchParams(location.search);
  return params.get("tenant");
}

function isIpAddress(host: string): boolean {
  return /^\d{1,3}(\.\d{1,3}){3}$/.test(host);
}
