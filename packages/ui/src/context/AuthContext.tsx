import { createContext, type ReactNode, useCallback, useContext, useEffect, useMemo } from "react";
import { type AuthSession, useAuthStore } from "../stores/authStore";

/**
 * The two HTTP calls AuthProvider needs, injected rather than imported directly from
 * `@peoplise/api-client`: keeps this package free of a dependency on a specific backend
 * client, and makes the provider trivially testable with a fake adapter.
 */
export interface AuthAdapter {
  login: (email: string, password: string) => Promise<AuthSession>;
  refresh: (refreshToken: string) => Promise<AuthSession>;
}

interface AuthContextValue {
  session: AuthSession | null;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
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

  const login = useCallback(
    async (email: string, password: string) => {
      const next = await adapter.login(email, password);
      setSession(next);
    },
    [adapter, setSession],
  );

  const logout = useCallback(() => clearSession(), [clearSession]);

  const value = useMemo<AuthContextValue>(
    () => ({ session, isAuthenticated: session !== null, login, logout }),
    [session, login, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth must be used within an AuthProvider.");
  return context;
}
