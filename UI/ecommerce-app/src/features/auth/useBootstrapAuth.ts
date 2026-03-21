import { useEffect, useState } from 'react';
import axios from 'axios';
import { useAuthStore } from '@stores/authStore';
import { logger } from '@lib/logger';
import type { ApiResponse, AuthResponse } from '@types/api';

/**
 * Runs once on app boot.
 * Checks localStorage for a non-expired refresh token and, if found, calls
 * /api/auth/refresh to silently restore the session before any protected
 * route renders.
 *
 * Returns `{ ready: boolean }` — render the app tree only when ready === true
 * to prevent a flash of the login redirect for users who are actually signed in.
 */
export function useBootstrapAuth(): { ready: boolean } {
  const [ready, setReady] = useState(false);
  const { setTokens, setUser } = useAuthStore();

  useEffect(() => {
    async function hydrate() {
      const rt = localStorage.getItem('rt');
      const rtExp = localStorage.getItem('rt_exp');

      // No refresh token stored — nothing to hydrate
      if (!rt || !rtExp) {
        setReady(true);
        return;
      }

      // Check expiry before making a network call
      if (new Date(rtExp) <= new Date()) {
        logger.debug('Refresh token expired — skipping hydration');
        localStorage.removeItem('rt');
        localStorage.removeItem('rt_exp');
        setReady(true);
        return;
      }

      try {
        const { data } = await axios.post<ApiResponse<AuthResponse>>(
          `${import.meta.env.VITE_API_BASE_URL as string}/api/auth/refresh`,
          { accessToken: null, refreshToken: rt }
        );

        const authData = data.data;
        if (!authData) throw new Error('Empty refresh response');

        setTokens(authData.accessToken, authData.refreshToken, authData.refreshTokenExpiry);
        setUser({
          userId: authData.userId,
          name: authData.name,
          email: authData.email,
          role: authData.role,
        });

        logger.debug('Session hydrated on boot');
      } catch (err) {
        logger.warn('Boot hydration failed — clearing tokens', {
          reason: err instanceof Error ? err.message : String(err),
        });
        localStorage.removeItem('rt');
        localStorage.removeItem('rt_exp');
      } finally {
        setReady(true);
      }
    }

    void hydrate();
  }, [setTokens, setUser]);

  return { ready };
}
