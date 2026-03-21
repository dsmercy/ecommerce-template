import { RouterProvider } from 'react-router-dom';
import { QueryClientProvider } from '@tanstack/react-query';
import { Toaster } from 'sonner';

import { router } from '@/router';
import { queryClient } from '@lib/queryClient';
import { useBootstrapAuth } from '@features/auth/useBootstrapAuth';
import { PageSkeleton } from '@components/shared/PageSkeleton';

/**
 * AppBootstrapper runs the silent-refresh hydration on mount.
 * It renders nothing until hydration is resolved so protected routes
 * do not flash a login redirect for users who are genuinely signed in.
 */
function AppBootstrapper() {
  const { ready } = useBootstrapAuth();
  if (!ready) return <PageSkeleton />;
  return <RouterProvider router={router} />;
}

/**
 * App — root component.
 * Providers are ordered outside-in: QueryClient → Router → Toast.
 */
export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AppBootstrapper />
      {/* Toast notifications — success=auto-dismiss 3s, error=manual dismiss */}
      <Toaster
        position="top-right"
        richColors
        toastOptions={{
          duration: 3000,
          classNames: {
            error: 'font-medium',
          },
        }}
      />
    </QueryClientProvider>
  );
}
