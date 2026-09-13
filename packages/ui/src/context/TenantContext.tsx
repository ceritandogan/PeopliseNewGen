import { createContext, type ReactNode, useContext, useEffect } from "react";
import { resolveTenantSlugFromLocation, useTenantStore } from "../stores/tenantStore";

interface TenantContextValue {
  tenantSlug: string | null;
}

const TenantContext = createContext<TenantContextValue | null>(null);

/** Resolves the tenant from the URL once on mount — see `resolveTenantSlugFromLocation` for the subdomain/query-param rules. */
export function TenantProvider({ children }: { children: ReactNode }) {
  const tenantSlug = useTenantStore((state) => state.tenantSlug);
  const setTenantSlug = useTenantStore((state) => state.setTenantSlug);

  useEffect(() => {
    const resolved = resolveTenantSlugFromLocation(window.location);
    if (resolved) setTenantSlug(resolved);
  }, [setTenantSlug]);

  return <TenantContext.Provider value={{ tenantSlug }}>{children}</TenantContext.Provider>;
}

export function useTenant(): TenantContextValue {
  const context = useContext(TenantContext);
  if (!context) throw new Error("useTenant must be used within a TenantProvider.");
  return context;
}
