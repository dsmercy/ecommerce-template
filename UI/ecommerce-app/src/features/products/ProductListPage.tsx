import { useCallback, useMemo, useState } from 'react';
import { useSearchParams, useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { SlidersHorizontal } from 'lucide-react';
import { Button } from '@components/ui/button';
import { ProductCard } from '@components/shared/ProductCard';
import { ProductGridSkeleton } from '@components/shared/ProductCardSkeleton';
import { EmptyState } from '@components/shared/EmptyState';
import { Pagination } from '@components/shared/Pagination';
import { FilterSidebar, type FilterState } from './FilterSidebar';
import { getProducts } from '@api/endpoints/products';
import type { ProductListItem } from '@types/api';

// ─── URL param helpers ────────────────────────────────────────────────────────

function filtersFromParams(params: URLSearchParams): FilterState {
  return {
    categoryId: params.get('categoryId') ? Number(params.get('categoryId')) : undefined,
    minPrice: params.get('minPrice') ? Number(params.get('minPrice')) : undefined,
    maxPrice: params.get('maxPrice') ? Number(params.get('maxPrice')) : undefined,
    brand: params.get('brand') ?? undefined,
    sort: (params.get('sort') as FilterState['sort']) ?? 'newest',
  };
}

function paramsFromFilters(filters: FilterState, page: number): URLSearchParams {
  const p = new URLSearchParams();
  if (filters.categoryId) p.set('categoryId', String(filters.categoryId));
  if (filters.minPrice != null) p.set('minPrice', String(filters.minPrice));
  if (filters.maxPrice != null) p.set('maxPrice', String(filters.maxPrice));
  if (filters.brand) p.set('brand', filters.brand);
  if (filters.sort && filters.sort !== 'newest') p.set('sort', filters.sort);
  if (page > 1) p.set('page', String(page));
  return p;
}

// ─── ProductListPage ──────────────────────────────────────────────────────────

export default function ProductListPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const { slug } = useParams<{ slug?: string }>();
  const [filtersOpen, setFiltersOpen] = useState(false);

  const page = Number(searchParams.get('page') ?? 1);
  const filters = useMemo(() => filtersFromParams(searchParams), [searchParams]);

  const queryParams = useMemo(
    () => ({
      ...filters,
      page,
      pageSize: 20,
      // If routed via /categories/:slug we'll let the sidebar handle categoryId
    }),
    [filters, page]
  );

  const { data, isPending, isError } = useQuery({
    queryKey: ['products', queryParams],
    queryFn: () => getProducts(queryParams),
  });

  const products = data?.data?.items ?? [];
  const totalPages = data?.data?.totalPages ?? 1;
  const totalCount = data?.data?.totalCount ?? 0;

  const handleFiltersChange = useCallback(
    (newFilters: FilterState) => {
      setSearchParams(paramsFromFilters(newFilters, 1));
    },
    [setSearchParams]
  );

  const handlePageChange = useCallback(
    (newPage: number) => {
      setSearchParams(paramsFromFilters(filters, newPage));
      window.scrollTo({ top: 0, behavior: 'smooth' });
    },
    [filters, setSearchParams]
  );

  const handleReset = useCallback(() => {
    setSearchParams(new URLSearchParams());
  }, [setSearchParams]);

  const heading = slug ? `Category: ${slug}` : 'All Products';

  return (
    <div className="mx-auto max-w-screen-xl px-4 py-8 md:px-8">
      {/* Page header */}
      <div className="mb-6 flex items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">{heading}</h1>
          {!isPending && (
            <p className="mt-1 text-sm text-muted-foreground">
              {totalCount.toLocaleString()} product{totalCount !== 1 ? 's' : ''}
            </p>
          )}
        </div>

        {/* Mobile filter toggle */}
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

      <div className="flex gap-6">
        {/* Sidebar */}
        <FilterSidebar
          filters={filters}
          onFiltersChange={handleFiltersChange}
          onReset={handleReset}
          isOpen={filtersOpen}
          onClose={() => setFiltersOpen(false)}
          className="hidden md:block"
        />

        {/* Mobile sidebar (portal-like, positioned fixed) */}
        <div className="md:hidden">
          <FilterSidebar
            filters={filters}
            onFiltersChange={handleFiltersChange}
            onReset={handleReset}
            isOpen={filtersOpen}
            onClose={() => setFiltersOpen(false)}
          />
        </div>

        {/* Main content */}
        <div className="flex-1 min-w-0">
          {isPending ? (
            <ProductGridSkeleton count={12} />
          ) : isError ? (
            <EmptyState
              title="Failed to load products"
              description="Something went wrong. Please try again."
              action={{ label: 'Retry', onClick: handleReset }}
            />
          ) : products.length === 0 ? (
            <EmptyState
              title="No products found"
              description="Try adjusting your filters to find what you're looking for."
              action={{ label: 'Reset filters', onClick: handleReset }}
            />
          ) : (
            <>
              <ProductGrid products={products} />
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
    </div>
  );
}

// ─── ProductGrid ──────────────────────────────────────────────────────────────

function ProductGrid({ products }: { products: ProductListItem[] }) {
  return (
    <div
      className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4"
      aria-label="Product list"
    >
      {products.map((product) => (
        <ProductCard key={product.id} product={product} />
      ))}
    </div>
  );
}
