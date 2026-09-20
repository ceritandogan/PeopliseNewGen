import { createContext, type ReactNode, useCallback, useContext, useEffect, useMemo } from "react";
import { type AuthSession, useAuthStore } from "../stores/authStore";

/**
 * The HTTP calls AuthProvider needs, injected rather than imported directly from
 * `@peoplise/api-client`: keeps this package free of a dependency on a specific backend
 * client, and makes the provider trivially testable with a fake adapter. Login is two
 * phases, not one call, because it's a redirect: `beginLogin` navigates the whole page
 * away and never resolves in practice; `completeLogin` runs on the callback page once
 * the browser lands back with an authorization code. See ADR 0002.
 */
export interface AuthAdapter {
  beginLogin: (returnTo: string) => Promise<void>;
  completeLogin: (params: URLSearchParams) => Promise<{ session: AuthSession; returnTo: string }>;
  refresh: (refreshToken: string) => Promise<AuthSession>;
  logout: () => Promise<void>;
}

interface AuthContextValue {
  session: AuthSession | null;
  isAuthenticated: boolean;
  beginLogin: (returnTo: string) => Promise<void>;
  completeLogin: (params: URLSearchParams) => Promise<string>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

/** Refresh this many ms before actual expiry, so a slow network round-trip doesn't land after the token's already dead. */
const REFRESH_MARGIN_MS = 30_000;

export interface AuthProviderProps {
  children: ReactNode;
  adapter: AuthAdapter;
}

/**
 * "Auth Context: JWT token yönetimi, otomatik refresh, 401 redirect." The redirect part
 * is deliberately *not* imperative here: on an unrecoverable 401 the Axios interceptor
 * (in `@peoplise/api-client`) clears the session via `useAuthStore.getState().clearSession()`,
 * and `ProtectedRoute` reacts to `isAuthenticated` turning false — no direct coupling
 * between the HTTP layer and the router needed.
 */
export function AuthProvider({ children, adapter }: AuthProviderProps) {
  const session = useAuthStore((state) => state.session);
  const setSession = useAuthStore((state) => state.setSession);
  const clearSession = useAuthStore((state) => state.clearSession);

  useEffect(() => {
    if (!session) return;

    const delay = Math.max(session.expiresAt - Date.now() - REFRESH_MARGIN_MS, 0);
    const timer = setTimeout(async () => {
      try {
        const next = await adapter.refresh(session.refreshToken);
        setSession(next);
      } catch {
        clearSession();
      }
    }, delay);

    return () => clearTimeout(timer);
  }, [session, adapter, setSession, clearSession]);

  const beginLogin = useCallback((returnTo: string) => adapter.beginLogin(returnTo), [adapter]);

  const completeLogin = useCallback(
    async (params: URLSearchParams) => {
      const { session: next, returnTo } = await adapter.completeLogin(params);
      setSession(next);
      return returnTo;
    },
    [adapter, setSession],
  );

  const logout = useCallback(() => {
    clearSession();
    void adapter.logout();
  }, [adapter, clearSession]);

  const value = useMemo<AuthContextValue>(
    () => ({ session, isAuthenticated: session !== null, beginLogin, completeLogin, logout }),
    [session, beginLogin, completeLogin, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth must be used within an AuthProvider.");
  return context;
}
