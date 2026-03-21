import { QueryClient } from '@tanstack/react-query';
import { logger } from '@lib/logger';
import { toast } from 'sonner';

// ─── Helper: extract API error message ───────────────────────────────────────

function extractApiMessage(error: unknown): string | null {
  if (
    error &&
    typeof error === 'object' &&
    'response' in error &&
    (error as { response?: { data?: { message?: string } } }).response?.data?.message
  ) {
    return (error as { response: { data: { message: string } } }).response.data.message;
  }
  return null;
}

// ─── QueryClient ──────────────────────────────────────────────────────────────

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1, // one automatic retry for transient network glitches
      staleTime: 1000 * 60 * 5, // data stays fresh for 5 minutes
      refetchOnWindowFocus: false,
    },
    mutations: {
      retry: 0, // never retry mutations — side effects must not double-fire
      onError: (error: unknown) => {
        const message =
          extractApiMessage(error) ?? 'Something went wrong. Please try again.';
        toast.error(message);
        logger.error('Mutation failed', {
          message: error instanceof Error ? error.message : String(error),
        });
      },
    },
  },
});

// Global query error handler (TanStack Query v5 approach via queryCache)
queryClient.getQueryCache().subscribe((event) => {
  if (event.type === 'updated' && event.query.state.status === 'error') {
    const error = event.query.state.error;
    logger.warn('Query failed', {
      queryKey: JSON.stringify(event.query.queryKey),
      message: error instanceof Error ? error.message : String(error),
    });
  }
});
