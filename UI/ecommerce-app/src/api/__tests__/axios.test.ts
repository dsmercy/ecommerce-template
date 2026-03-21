import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import axios from 'axios';
import MockAdapter from 'axios-mock-adapter';
import { useAuthStore } from '@stores/authStore';

/**
 * NOTE: We test the interceptor behavior by importing the api instance.
 * The module is re-imported fresh per suite to reset the isRefreshing flag.
 *
 * Install: npm install --save-dev axios-mock-adapter
 */

describe('Axios interceptor — token refresh', () => {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  let mock: any;

  beforeEach(async () => {
    vi.resetModules();
    useAuthStore.setState({ accessToken: 'old.access.token', user: null });
    localStorage.setItem('rt', 'mock.refresh.token');
    localStorage.setItem('rt_exp', new Date(Date.now() + 86400000).toISOString());

    // Lazily set up mock after reset
    const MockAdapterModule = await import('axios-mock-adapter');
    const MockAdapterClass = MockAdapterModule.default;
    mock = new MockAdapterClass(axios);
  });

  afterEach(() => {
    mock?.restore();
    localStorage.clear();
    useAuthStore.setState({ accessToken: null, user: null });
  });

  it('attaches Authorization header when access token is present', async () => {
    // Re-import api after module reset
    const { default: api } = await import('@api/axios');
    mock.onGet('/api/test').reply((config: { headers?: Record<string, string> }) => {
      const auth = config.headers?.['Authorization'] ?? config.headers?.['authorization'];
      if (auth === 'Bearer old.access.token') return [200, { success: true, data: 'ok' }];
      return [401, {}];
    });

    const res = await api.get('/api/test');
    expect(res.status).toBe(200);
  });

  it('does not attach Authorization when no token', async () => {
    useAuthStore.setState({ accessToken: null, user: null });
    const { default: api } = await import('@api/axios');

    mock.onGet('/api/public').reply((config: { headers?: Record<string, string> }) => {
      const hasAuth = !!(config.headers?.['Authorization'] || config.headers?.['authorization']);
      return [200, { hasAuth }];
    });

    const res = await api.get('/api/public');
    expect(res.data.hasAuth).toBe(false);
  });

  it('concurrent 401s trigger only ONE refresh call, not multiple', async () => {
    const { default: api } = await import('@api/axios');

    let refreshCallCount = 0;
    const newToken = 'new.access.token.xyz';

    mock
      .onPost('/api/auth/refresh')
      .reply(() => {
        refreshCallCount++;
        return [
          200,
          {
            success: true,
            data: {
              accessToken: newToken,
              refreshToken: 'new.refresh.token',
              refreshTokenExpiry: new Date(Date.now() + 86400000).toISOString(),
              userId: 1,
              name: 'Test',
              email: 'test@test.com',
              role: 'CUSTOMER',
            },
          },
        ];
      });

    // First call returns 401, retry returns 200 with new token
    mock
      .onGet('/api/protected-a')
      .replyOnce(401, {})
      .onGet('/api/protected-a')
      .reply(200, { data: 'a-result' });

    mock
      .onGet('/api/protected-b')
      .replyOnce(401, {})
      .onGet('/api/protected-b')
      .reply(200, { data: 'b-result' });

    // Fire both concurrently
    const [resA, resB] = await Promise.all([
      api.get('/api/protected-a'),
      api.get('/api/protected-b'),
    ]);

    // Both requests should succeed
    expect(resA.status).toBe(200);
    expect(resB.status).toBe(200);

    // The refresh endpoint must have been called exactly once
    expect(refreshCallCount).toBe(1);
  });

  it('redirects to /login and calls logout when refresh fails', async () => {
    const { default: api } = await import('@api/axios');

    mock.onPost('/api/auth/refresh').reply(401, { message: 'Refresh token expired' });
    mock.onGet('/api/protected').replyOnce(401, {});

    const logoutSpy = vi.spyOn(useAuthStore.getState(), 'logout');

    await expect(api.get('/api/protected')).rejects.toBeDefined();

    expect(logoutSpy).toHaveBeenCalledOnce();
  });
});
