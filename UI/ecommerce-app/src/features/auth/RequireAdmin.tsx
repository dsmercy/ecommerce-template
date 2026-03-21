import { Navigate } from 'react-router-dom';
import { useAuthStore } from '@stores/authStore';

interface RequireAdminProps {
  children: React.ReactNode;
}

/**
 * Wraps admin-only routes.
 * Redirects to home if the authenticated user is not ADMIN.
 * RequireAuth must wrap this — RequireAdmin assumes a token is already present.
 */
export function RequireAdmin({ children }: RequireAdminProps) {
  const user = useAuthStore((s) => s.user);

  if (user?.role !== 'ADMIN') {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
}
