/**
 * Full-page skeleton shown by React.lazy Suspense boundaries while
 * a route-level code-split chunk is being fetched.
 */
export function PageSkeleton() {
  return (
    <div className="mx-auto w-full max-w-screen-xl px-4 py-8" aria-busy="true" aria-label="Loading page…">
      {/* Simulated page header */}
      <div className="mb-6 h-8 w-48 animate-pulse rounded-md bg-gray-200" />

      {/* Simulated content rows */}
      <div className="space-y-4">
        {Array.from({ length: 6 }).map((_, i) => (
          <div key={i} className="flex gap-4">
            <div className="h-20 w-20 flex-shrink-0 animate-pulse rounded-md bg-gray-200" />
            <div className="flex-1 space-y-2 py-1">
              <div className="h-4 w-3/4 animate-pulse rounded bg-gray-200" />
              <div className="h-4 w-1/2 animate-pulse rounded bg-gray-200" />
              <div className="h-4 w-1/4 animate-pulse rounded bg-gray-200" />
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
