import { create } from 'zustand';

// ─── Types ────────────────────────────────────────────────────────────────────

export interface AuthUser {
  userId: number;
  name: string;
  email: string;
  role: 'ADMIN' | 'CUSTOMER';
}

interface AuthState {
  /** Lives in memory only — never persisted to localStorage.
   *  XSS cannot read a Zustand in-memory value. */
  accessToken: string | null;
  user: AuthUser | null;

  /** Actions */
  setTokens: (accessToken: string, refreshToken: string, refreshTokenExpiry: string) => void;
  setUser: (user: AuthUser) => void;
  logout: () => void;
}

// ─── Store ────────────────────────────────────────────────────────────────────

export const useAuthStore = create<AuthState>()((set) => ({
  accessToken: null,
  user: null,

  /**
   * Called after a successful login, register, or silent token refresh.
   * - accessToken: stored in memory only (this store)
   * - refreshToken: stored in localStorage key "rt"
   * - refreshTokenExpiry: stored in localStorage key "rt_exp"
   */
  setTokens: (accessToken, refreshToken, refreshTokenExpiry) => {
    localStorage.setItem('rt', refreshToken);
    localStorage.setItem('rt_exp', refreshTokenExpiry);
    set({ accessToken });
  },

  /** Set or update the authenticated user's profile. */
  setUser: (user) => set({ user }),

  /**
   * Clear all auth state.
   * Removes refresh token from localStorage and wipes in-memory access token.
   */
  logout: () => {
    localStorage.removeItem('rt');
    localStorage.removeItem('rt_exp');
    set({ accessToken: null, user: null });
  },
}));
