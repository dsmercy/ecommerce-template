import { describe, it, expect, beforeEach, vi } from 'vitest';
import { useAuthStore } from '@stores/authStore';

// ─── Helpers ──────────────────────────────────────────────────────────────────

const MOCK_USER = {
  userId: 1,
  name: 'Alice Test',
  email: 'alice@example.com',
  role: 'CUSTOMER' as const,
};

const MOCK_TOKENS = {
  accessToken: 'at.mock.abc123',
  refreshToken: 'rt.mock.xyz789',
  refreshTokenExpiry: new Date(Date.now() + 1000 * 60 * 60 * 24 * 7).toISOString(),
};

// ─── Tests ────────────────────────────────────────────────────────────────────

describe('authStore', () => {
  beforeEach(() => {
    // Reset store to initial state and clear localStorage before each test
    useAuthStore.setState({ accessToken: null, user: null });
    localStorage.clear();
  });

  // ── Initial state ──

  it('starts with no token and no user', () => {
    const { accessToken, user } = useAuthStore.getState();
    expect(accessToken).toBeNull();
    expect(user).toBeNull();
  });

  // ── setTokens ──

  it('setTokens stores accessToken in memory', () => {
    useAuthStore.getState().setTokens(
      MOCK_TOKENS.accessToken,
      MOCK_TOKENS.refreshToken,
      MOCK_TOKENS.refreshTokenExpiry
    );
    expect(useAuthStore.getState().accessToken).toBe(MOCK_TOKENS.accessToken);
  });

  it('setTokens writes refreshToken to localStorage key "rt"', () => {
    useAuthStore.getState().setTokens(
      MOCK_TOKENS.accessToken,
      MOCK_TOKENS.refreshToken,
      MOCK_TOKENS.refreshTokenExpiry
    );
    expect(localStorage.getItem('rt')).toBe(MOCK_TOKENS.refreshToken);
  });

  it('setTokens writes refreshTokenExpiry to localStorage key "rt_exp"', () => {
    useAuthStore.getState().setTokens(
      MOCK_TOKENS.accessToken,
      MOCK_TOKENS.refreshToken,
      MOCK_TOKENS.refreshTokenExpiry
    );
    expect(localStorage.getItem('rt_exp')).toBe(MOCK_TOKENS.refreshTokenExpiry);
  });

  it('setTokens does NOT expose accessToken in localStorage', () => {
    useAuthStore.getState().setTokens(
      MOCK_TOKENS.accessToken,
      MOCK_TOKENS.refreshToken,
      MOCK_TOKENS.refreshTokenExpiry
    );
    // No localStorage key should hold the access token
    const allValues = Object.values(localStorage).join('|');
    expect(allValues).not.toContain(MOCK_TOKENS.accessToken);
  });

  // ── setUser ──

  it('setUser stores user profile', () => {
    useAuthStore.getState().setUser(MOCK_USER);
    expect(useAuthStore.getState().user).toEqual(MOCK_USER);
  });

  it('setUser role can be ADMIN', () => {
    useAuthStore.getState().setUser({ ...MOCK_USER, role: 'ADMIN' });
    expect(useAuthStore.getState().user?.role).toBe('ADMIN');
  });

  // ── logout ──

  it('logout clears accessToken in memory', () => {
    useAuthStore.getState().setTokens(
      MOCK_TOKENS.accessToken,
      MOCK_TOKENS.refreshToken,
      MOCK_TOKENS.refreshTokenExpiry
    );
    useAuthStore.getState().logout();
    expect(useAuthStore.getState().accessToken).toBeNull();
  });

  it('logout clears user', () => {
    useAuthStore.getState().setUser(MOCK_USER);
    useAuthStore.getState().logout();
    expect(useAuthStore.getState().user).toBeNull();
  });

  it('logout removes "rt" from localStorage', () => {
    localStorage.setItem('rt', MOCK_TOKENS.refreshToken);
    useAuthStore.getState().logout();
    expect(localStorage.getItem('rt')).toBeNull();
  });

  it('logout removes "rt_exp" from localStorage', () => {
    localStorage.setItem('rt_exp', MOCK_TOKENS.refreshTokenExpiry);
    useAuthStore.getState().logout();
    expect(localStorage.getItem('rt_exp')).toBeNull();
  });

  it('logout is idempotent — calling twice does not throw', () => {
    expect(() => {
      useAuthStore.getState().logout();
      useAuthStore.getState().logout();
    }).not.toThrow();
  });

  // ── Token hydration simulation ──

  it('token hydration pattern: setTokens then setUser restores full session', () => {
    useAuthStore.getState().setTokens(
      MOCK_TOKENS.accessToken,
      MOCK_TOKENS.refreshToken,
      MOCK_TOKENS.refreshTokenExpiry
    );
    useAuthStore.getState().setUser(MOCK_USER);

    const state = useAuthStore.getState();
    expect(state.accessToken).toBe(MOCK_TOKENS.accessToken);
    expect(state.user?.email).toBe(MOCK_USER.email);
    expect(localStorage.getItem('rt')).toBe(MOCK_TOKENS.refreshToken);
  });

  // ── getState() is accessible outside React (for axios interceptor) ──

  it('getState().accessToken is accessible synchronously (used by axios interceptor)', () => {
    useAuthStore.getState().setTokens(
      MOCK_TOKENS.accessToken,
      MOCK_TOKENS.refreshToken,
      MOCK_TOKENS.refreshTokenExpiry
    );
    // Simulate how the Axios interceptor reads it outside a component
    const token = useAuthStore.getState().accessToken;
    expect(token).toBe(MOCK_TOKENS.accessToken);
  });
});
