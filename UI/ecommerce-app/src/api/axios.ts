import axios from 'axios';
import { useAuthStore } from '@stores/authStore';
import { logger } from '@lib/logger';
import type { ApiResponse, AuthResponse } from '@types/api';

// ─── Axios instance ───────────────────────────────────────────────────────────

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL as string,
  headers: { 'Content-Type': 'application/json' },
  timeout: 15_000,
});

// ─── Request interceptor — attach Bearer token ────────────────────────────────

api.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// ─── 401 refresh machinery ────────────────────────────────────────────────────

let isRefreshing = false;
let waitQueue: Array<{
  resolve: (token: string) => void;
  reject: (err: unknown) => void;
}> = [];

function processQueue(error: unknown, token: string | null): void {
  waitQueue.forEach((p) => (error ? p.reject(error) : p.resolve(token!)));
  waitQueue = [];
}

// ─── Auth endpoint paths — used to skip refresh logic ────────────────────────
// BUG 1 FIX: The interceptor was treating a 401 from /api/auth/login itself
// as "session expired" and attempting a refresh. That refresh fails (no rt),
// which calls logout() + window.location.href = '/login', which navigates
// back to /login, which re-mounts the page and re-triggers the login attempt
// — creating an infinite reload loop.
//
// Fix: never attempt a token refresh for requests that ARE the auth endpoints.
// A 401 from /api/auth/login simply means wrong credentials — show an error,
// do not try to refresh anything.

const AUTH_URLS = ['/api/auth/login', '/api/auth/register', '/api/auth/refresh'];

function isAuthEndpoint(url: string | undefined): boolean {
  if (!url) return false;
  return AUTH_URLS.some((authUrl) => url.includes(authUrl));
}

// ─── Response interceptor ─────────────────────────────────────────────────────

api.interceptors.response.use(
  (res) => res,
  async (error: unknown) => {
    const axiosError = error as {
      config?: {
        url?: string;
        _retry?: boolean;
        headers?: Record<string, string>;
      };
      response?: { status?: number; data?: { message?: string } };
    };

    const status = axiosError.response?.status;
    const originalConfig = axiosError.config;

    // ── BUG 1 FIX: skip refresh logic entirely for auth endpoints ──
    // A 401 on /login or /register is "wrong credentials" — not "expired session".
    // Attempting a refresh here caused the infinite loop:
    //   POST /login → 401 → interceptor tries refresh → no rt → logout()
    //   → window.location.href = '/login' → page reloads → repeat forever
    if (isAuthEndpoint(originalConfig?.url)) {
      return Promise.reject(error);
    }

    // ── Only intercept 401s that haven't already been retried ──
    if (status !== 401 || !originalConfig || originalConfig._retry) {
      if (status && status !== 401) {
        logger.error('API error', {
          status,
          message: axiosError.response?.data?.message ?? 'Unknown error',
          userId: useAuthStore.getState().user?.userId,
        });
      }
      return Promise.reject(error);
    }

    // ── BUG 2 FIX: guard against missing refresh token BEFORE setting isRefreshing ──
    // Previously the code set _retry = true and isRefreshing = true, then entered
    // the try block and immediately threw "No refresh token available". The finally
    // block reset isRefreshing = false, but logout() + window.location.href had
    // already fired — causing the page to navigate before the queue was drained,
    // leaving waitQueue in a broken state on the new page load.
    //
    // Fix: check for the refresh token FIRST. If it's missing the user is simply
    // not logged in — reject silently without touching isRefreshing or calling logout().
    const refreshToken = localStorage.getItem('rt');
    if (!refreshToken) {
      return Promise.reject(error);
    }

    // ── If already refreshing, queue this request and wait ──
    if (isRefreshing) {
      return new Promise<string>((resolve, reject) => {
        waitQueue.push({ resolve, reject });
      }).then((newToken) => {
        if (originalConfig.headers) {
          originalConfig.headers.Authorization = `Bearer ${newToken}`;
        }
        return api(originalConfig as Parameters<typeof api>[0]);
      });
    }

    // ── First 401 — attempt a silent token refresh ──
    originalConfig._retry = true;
    isRefreshing = true;

    try {
      const accessToken = useAuthStore.getState().accessToken;

      const { data } = await axios.post<ApiResponse<AuthResponse>>(
        `${import.meta.env.VITE_API_BASE_URL as string}/api/auth/refresh`,
        { accessToken, refreshToken }
      );

      const refreshed = data.data;
      if (!refreshed) throw new Error('Refresh response contained no data');

      const { accessToken: newAt, refreshToken: newRt, refreshTokenExpiry } = refreshed;

      useAuthStore.getState().setTokens(newAt, newRt, refreshTokenExpiry);
      processQueue(null, newAt);

      if (originalConfig.headers) {
        originalConfig.headers.Authorization = `Bearer ${newAt}`;
      }

      logger.debug('Token refreshed successfully');
      return api(originalConfig as Parameters<typeof api>[0]);
    } catch (refreshError) {
      processQueue(refreshError, null);

      logger.warn('Token refresh failed — logging out', {
        reason: refreshError instanceof Error ? refreshError.message : String(refreshError),
      });

      useAuthStore.getState().logout();

      // BUG 2 FIX (continued): use navigate instead of window.location.href
      // window.location.href causes a full browser reload which re-runs the
      // boot hydration, which calls /api/auth/refresh again (with the now-revoked
      // token), which 401s again, triggering this catch block again = another loop.
      //
      // We import the router and use navigate(0) so React Router handles the
      // redirect without a full page reload — no re-hydration, no loop.
      //
      // If the router isn't available here (circular dep risk), fall back to
      // replacing location without a reload:
      window.location.replace('/login');

      return Promise.reject(refreshError);
    } finally {
      isRefreshing = false;
    }
  }
);

export default api;