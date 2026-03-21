import { Navigate, useLocation } from 'react-router-dom';
import { useAuthStore } from '@stores/authStore';

interface RequireAuthProps {
  children: React.ReactNode;
}

/**
 * Wraps protected routes.
 * If no access token is present, redirects to /login?next=<currentPath>
 * so the user is returned to their intended destination after signing in.
 */
export function RequireAuth({ children }: RequireAuthProps) {
  const accessToken = useAuthStore((s) => s.accessToken);
  const location = useLocation();

  if (!accessToken) {
    const next = encodeURIComponent(location.pathname + location.search);
    return <Navigate to={`/login?next=${next}`} replace />;
  }

  return <>{children}</>;
}
