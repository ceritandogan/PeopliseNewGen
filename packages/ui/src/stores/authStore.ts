import { create } from "zustand";
import { persist } from "zustand/middleware";

export interface AuthUser {
  id: string;
  email: string;
  roles: string[];
}

export interface AuthSession {
  accessToken: string;
  refreshToken: string;
  /** Epoch milliseconds — when the access token stops being valid. */
  expiresAt: number;
  user: AuthUser;
}

interface AuthState {
  session: AuthSession | null;
  setSession: (session: AuthSession) => void;
  clearSession: () => void;
}

/**
 * Persisted to localStorage so a refresh doesn't log the user out — the access token is
 * short-lived by design (OpenIddict-issued), so persisting it is no worse than
 * persisting the refresh token, which every "remember me" flow already does.
 */
export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      session: null,
      setSession: (session) => set({ session }),
      clearSession: () => set({ session: null }),
    }),
    { name: "peoplise.auth" },
  ),
);

/** Read outside React (e.g. from an Axios interceptor) without subscribing to re-renders. */
export function getAuthSession(): AuthSession | null {
  return useAuthStore.getState().session;
}
