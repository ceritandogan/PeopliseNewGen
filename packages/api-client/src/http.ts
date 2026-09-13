import axios, { type AxiosError, type InternalAxiosRequestConfig } from "axios";
import { getAuthSession, useAuthStore, useTenantStore } from "@peoplise/ui";
import { refreshTokenGrant } from "./tokenEndpoint";

/**
 * The shared HTTP client every resource module (`./resources/*`) calls through.
 * "Axios (HTTP client, interceptor ile JWT refresh)" from the architecture doc's stack —
 * both interceptors live here so every request gets them automatically.
 */
export const httpClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000",
});

httpClient.interceptors.request.use((config) => {
  const session = getAuthSession();
  if (session) {
    config.headers.Authorization = `Bearer ${session.accessToken}`;
  }

  const tenantSlug = useTenantStore.getState().tenantSlug;
  if (tenantSlug) {
    config.headers["X-Tenant"] = tenantSlug;
  }

  return config;
});

type RetriableRequestConfig = InternalAxiosRequestConfig & { _retriedAfterRefresh?: boolean };

// Coalesces concurrent 401s into a single refresh call rather than firing one refresh
// request per failed request.
let inFlightRefresh: Promise<string> | null = null;

httpClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const original = error.config as RetriableRequestConfig | undefined;
    const session = getAuthSession();

    if (error.response?.status !== 401 || !original || original._retriedAfterRefresh || !session) {
      throw error;
    }

    original._retriedAfterRefresh = true;

    try {
      inFlightRefresh ??= refreshTokenGrant(session.refreshToken).then((next) => {
        useAuthStore.getState().setSession(next);
        return next.accessToken;
      });

      const accessToken = await inFlightRefresh;
      original.headers.set("Authorization", `Bearer ${accessToken}`);
      return httpClient(original);
    } catch (refreshError) {
      // Unrecoverable: clear the session and let ProtectedRoute's isAuthenticated
      // check redirect to login — see AuthProvider's remarks on the 401 redirect flow.
      useAuthStore.getState().clearSession();
      throw refreshError;
    } finally {
      inFlightRefresh = null;
    }
  },
);
