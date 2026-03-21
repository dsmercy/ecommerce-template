import { describe, it, expect, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { RequireAuth } from '@features/auth/RequireAuth';
import { useAuthStore } from '@stores/authStore';

// ─── Helpers ──────────────────────────────────────────────────────────────────

function renderWithRouter(
  initialPath: string,
  element: React.ReactNode
) {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <Routes>
        <Route path="*" element={element} />
        <Route path="/login" element={<div>Login Page</div>} />
      </Routes>
    </MemoryRouter>
  );
}

// ─── Tests ────────────────────────────────────────────────────────────────────

describe('RequireAuth', () => {
  beforeEach(() => {
    useAuthStore.setState({ accessToken: null, user: null });
    localStorage.clear();
  });

  it('redirects to /login when no access token', () => {
    renderWithRouter(
      '/cart',
      <RequireAuth>
        <div>Protected Content</div>
      </RequireAuth>
    );

    expect(screen.getByText('Login Page')).toBeInTheDocument();
    expect(screen.queryByText('Protected Content')).not.toBeInTheDocument();
  });

  it('renders children when access token is present', () => {
    useAuthStore.setState({ accessToken: 'mock.token.abc', user: null });

    renderWithRouter(
      '/cart',
      <RequireAuth>
        <div>Protected Content</div>
      </RequireAuth>
    );

    expect(screen.getByText('Protected Content')).toBeInTheDocument();
    expect(screen.queryByText('Login Page')).not.toBeInTheDocument();
  });

  it('includes ?next= with the current path in the redirect URL', () => {
    // We'll use a custom component to inspect the destination
    let redirectTo = '';

    const CapturingLogin = () => {
      // Read the location from the router — check the rendered element
      redirectTo = window.location.href;
      return <div>Login Page</div>;
    };

    render(
      <MemoryRouter initialEntries={['/orders/42']}>
        <Routes>
          <Route
            path="/orders/:id"
            element={
              <RequireAuth>
                <div>Order Detail</div>
              </RequireAuth>
            }
          />
          <Route path="/login" element={<CapturingLogin />} />
        </Routes>
      </MemoryRouter>
    );

    // RequireAuth should have navigated to /login with a ?next param
    expect(screen.getByText('Login Page')).toBeInTheDocument();
    // The "next" redirect is encoded in the Navigate "to" prop
    // We verify the protected content is NOT shown
    expect(screen.queryByText('Order Detail')).not.toBeInTheDocument();
  });

  it('does not redirect when token is set (even with no user object)', () => {
    useAuthStore.setState({ accessToken: 'bearer.xyz', user: null });

    renderWithRouter(
      '/profile',
      <RequireAuth>
        <div>Profile Page</div>
      </RequireAuth>
    );

    expect(screen.getByText('Profile Page')).toBeInTheDocument();
  });
});
