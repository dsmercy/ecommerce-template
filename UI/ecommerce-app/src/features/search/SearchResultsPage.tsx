import { useCallback, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Search, SlidersHorizontal } from 'lucide-react';
import { Button } from '@components/ui/button';
import { ProductCard } from '@components/shared/ProductCard';
import { ProductGridSkeleton } from '@components/shared/ProductCardSkeleton';
import { EmptyState } from '@components/shared/EmptyState';
import { Pagination } from '@components/shared/Pagination';
import { FilterSidebar, type FilterState } from '@features/products/FilterSidebar';
import { searchProducts } from '@api/endpoints/products';

// ─── SearchResultsPage ────────────────────────────────────────────────────────

export default function SearchResultsPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [filtersOpen, setFiltersOpen] = useState(false);

  const query = searchParams.get('q') ?? '';
  const page = Number(searchParams.get('page') ?? 1);

  const filters = useMemo<FilterState>(
    () => ({
      categoryId: searchParams.get('categoryId')
        ? Number(searchParams.get('categoryId'))
        : undefined,
      minPrice: searchParams.get('minPrice')
        ? Number(searchParams.get('minPrice'))
        : undefined,
      maxPrice: searchParams.get('maxPrice')
        ? Number(searchParams.get('maxPrice'))
        : undefined,
      brand: searchParams.get('brand') ?? undefined,
      sort: (searchParams.get('sort') as FilterState['sort']) ?? 'newest',
    }),
    [searchParams]
  );

  const { data, isPending } = useQuery({
    queryKey: ['search', query, filters, page],
    queryFn: () =>
      searchProducts({
        query,
        categoryId: filters.categoryId,
        minPrice: filters.minPrice,
        maxPrice: filters.maxPrice,
        page,
        pageSize: 20,
      }),
    enabled: query.length >= 2,
  });

  const products = data?.data?.items ?? [];
  const totalCount = data?.data?.totalCount ?? 0;
  const totalPages = data?.data?.totalPages ?? 1;

  const handleFiltersChange = useCallback(
    (newFilters: FilterState) => {
      const p = new URLSearchParams();
      p.set('q', query);
      if (newFilters.categoryId) p.set('categoryId', String(newFilters.categoryId));
      if (newFilters.minPrice != null) p.set('minPrice', String(newFilters.minPrice));
      if (newFilters.maxPrice != null) p.set('maxPrice', String(newFilters.maxPrice));
      if (newFilters.brand) p.set('brand', newFilters.brand);
      if (newFilters.sort && newFilters.sort !== 'newest') p.set('sort', newFilters.sort);
      setSearchParams(p);
    },
    [query, setSearchParams]
  );

  const handlePageChange = useCallback(
    (newPage: number) => {
      const p = new URLSearchParams(searchParams);
      p.set('page', String(newPage));
      setSearchParams(p);
      window.scrollTo({ top: 0, behavior: 'smooth' });
    },
    [searchParams, setSearchParams]
  );

  const handleReset = useCallback(() => {
    setSearchParams(new URLSearchParams({ q: query }));
  }, [query, setSearchParams]);

  return (
    <div className="mx-auto max-w-screen-xl px-4 py-8 md:px-8">
      {/* Header */}
      <div className="mb-6 flex items-center justify-between gap-4">
        <div>
          {query ? (
            <>
              <h1 className="text-2xl font-bold text-gray-900">
                Results for{' '}
                <span className="text-primary">
                  &ldquo;{query}&rdquo;
                </span>
              </h1>
              {!isPending && (
                <p className="mt-1 text-sm text-muted-foreground">
                  {totalCount.toLocaleString()} result
                  {totalCount !== 1 ? 's' : ''}
                </p>
              )}
            </>
          ) : (
            <h1 className="text-2xl font-bold text-gray-900">Search</h1>
          )}
        </div>

        <Button
          variant="outline"
          size="sm"
          className="md:hidden gap-2"
          onClick={() => setFiltersOpen(true)}
          aria-label="Open filters"
        >
          <SlidersHorizontal className="h-4 w-4" aria-hidden="true" />
          Filters
        </Button>
      </div>

      {query.length < 2 ? (
        <EmptyState
          icon={<Search className="h-7 w-7" aria-hidden="true" />}
          title="Start your search"
          description="Type at least 2 characters in the search bar above."
        />
      ) : (
        <div className="flex gap-6">
          {/* Sidebar — desktop inline */}
          <FilterSidebar
            filters={filters}
            onFiltersChange={handleFiltersChange}
            onReset={handleReset}
            isOpen={filtersOpen}
            onClose={() => setFiltersOpen(false)}
            className="hidden md:block"
          />

          {/* Sidebar — mobile drawer */}
          <div className="md:hidden">
            <FilterSidebar
              filters={filters}
              onFiltersChange={handleFiltersChange}
              onReset={handleReset}
              isOpen={filtersOpen}
              onClose={() => setFiltersOpen(false)}
            />
          </div>

          {/* Results */}
          <div className="flex-1 min-w-0">
            {isPending ? (
              <ProductGridSkeleton count={8} />
            ) : products.length === 0 ? (
              <EmptyState
                icon={<Search className="h-7 w-7" aria-hidden="true" />}
                title="No results found"
                description={`We couldn't find anything for "${query}". Try a different search term or adjust your filters.`}
                action={{ label: 'Reset filters', onClick: handleReset }}
              />
            ) : (
              <>
                <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
                  {products.map((product) => (
                    <ProductCard key={product.id} product={product} />
                  ))}
                </div>
                <Pagination
                  page={page}
                  totalPages={totalPages}
                  onPageChange={handlePageChange}
                  className="mt-10"
                />
              </>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
