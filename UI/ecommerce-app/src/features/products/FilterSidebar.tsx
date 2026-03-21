import { useState, useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import { ChevronDown, ChevronRight, SlidersHorizontal, X } from 'lucide-react';
import { Button } from '@components/ui/button';
import { Input } from '@components/ui/input';
import { Label } from '@components/ui/label';
import { Skeleton } from '@components/ui/skeleton';
import { cn } from '@lib/utils';
import { getCategories } from '@api/endpoints/products';
import type { Category } from '@types/api';

// ─── Filter state shape (URL-synced in parent) ────────────────────────────────

export interface FilterState {
  categoryId?: number;
  minPrice?: number;
  maxPrice?: number;
  brand?: string;
  sort?: 'newest' | 'price_asc' | 'price_desc';
}

interface FilterSidebarProps {
  filters: FilterState;
  onFiltersChange: (filters: FilterState) => void;
  onReset: () => void;
  isOpen: boolean;
  onClose: () => void;
  className?: string;
}

// ─── CategoryTree ─────────────────────────────────────────────────────────────

function CategoryTree({
  categories,
  selected,
  onSelect,
  depth = 0,
}: {
  categories: Category[];
  selected?: number;
  onSelect: (id: number | undefined) => void;
  depth?: number;
}) {
  const [expanded, setExpanded] = useState<Record<number, boolean>>({});

  return (
    <ul className={cn('space-y-0.5', depth > 0 && 'ml-4 mt-0.5')}>
      {categories.map((cat) => {
        const hasChildren = cat.children && cat.children.length > 0;
        const isExpanded = expanded[cat.id];
        const isSelected = selected === cat.id;

        return (
          <li key={cat.id}>
            <div className="flex items-center gap-1">
              {hasChildren && (
                <button
                  onClick={() => setExpanded((e) => ({ ...e, [cat.id]: !e[cat.id] }))}
                  className="flex h-5 w-5 flex-shrink-0 items-center justify-center rounded text-muted-foreground hover:text-foreground"
                  aria-label={isExpanded ? `Collapse ${cat.name}` : `Expand ${cat.name}`}
                >
                  {isExpanded ? (
                    <ChevronDown className="h-3 w-3" aria-hidden="true" />
                  ) : (
                    <ChevronRight className="h-3 w-3" aria-hidden="true" />
                  )}
                </button>
              )}
              <button
                onClick={() => onSelect(isSelected ? undefined : cat.id)}
                className={cn(
                  'flex-1 rounded px-2 py-1 text-left text-sm transition-colors',
                  !hasChildren && 'ml-5',
                  isSelected
                    ? 'bg-primary/10 font-medium text-primary'
                    : 'text-gray-700 hover:bg-gray-100'
                )}
              >
                {cat.name}
              </button>
            </div>
            {hasChildren && isExpanded && (
              <CategoryTree
                categories={cat.children}
                selected={selected}
                onSelect={onSelect}
                depth={depth + 1}
              />
            )}
          </li>
        );
      })}
    </ul>
  );
}

// ─── FilterSidebar ────────────────────────────────────────────────────────────

export function FilterSidebar({
  filters,
  onFiltersChange,
  onReset,
  isOpen,
  onClose,
  className,
}: FilterSidebarProps) {
  const { data: categoriesRes, isLoading: categoriesLoading } = useQuery({
    queryKey: ['categories'],
    queryFn: getCategories,
    staleTime: 1000 * 60 * 10,
  });

  const categories = categoriesRes?.data ?? [];

  const hasActiveFilters = useMemo(
    () =>
      !!(
        filters.categoryId ||
        filters.brand ||
        filters.minPrice != null ||
        filters.maxPrice != null
      ),
    [filters]
  );

  const [minInput, setMinInput] = useState(filters.minPrice?.toString() ?? '');
  const [maxInput, setMaxInput] = useState(filters.maxPrice?.toString() ?? '');

  function handlePriceApply() {
    const min = minInput ? parseFloat(minInput) : undefined;
    const max = maxInput ? parseFloat(maxInput) : undefined;
    onFiltersChange({ ...filters, minPrice: min, maxPrice: max });
  }

  function handlePriceKeyDown(e: React.KeyboardEvent) {
    if (e.key === 'Enter') handlePriceApply();
  }

  const sortOptions: { value: FilterState['sort']; label: string }[] = [
    { value: 'newest', label: 'Newest first' },
    { value: 'price_asc', label: 'Price: low → high' },
    { value: 'price_desc', label: 'Price: high → low' },
  ];

  const content = (
    <div className={cn('flex flex-col gap-6', className)}>
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <SlidersHorizontal className="h-4 w-4 text-muted-foreground" aria-hidden="true" />
          <span className="font-semibold text-gray-900">Filters</span>
        </div>
        <div className="flex items-center gap-2">
          {hasActiveFilters && (
            <button
              onClick={onReset}
              className="text-xs text-primary hover:underline underline-offset-2"
            >
              Reset all
            </button>
          )}
          <button
            onClick={onClose}
            className="md:hidden flex h-7 w-7 items-center justify-center rounded-full hover:bg-gray-100"
            aria-label="Close filters"
          >
            <X className="h-4 w-4" aria-hidden="true" />
          </button>
        </div>
      </div>

      {/* Sort */}
      <fieldset>
        <legend className="mb-2 text-xs font-semibold uppercase tracking-wider text-muted-foreground">
          Sort by
        </legend>
        <div className="space-y-1">
          {sortOptions.map((opt) => (
            <label key={opt.value} className="flex cursor-pointer items-center gap-2 group">
              <input
                type="radio"
                name="sort"
                value={opt.value}
                checked={filters.sort === opt.value}
                onChange={() => onFiltersChange({ ...filters, sort: opt.value })}
                className="accent-primary"
              />
              <span className="text-sm text-gray-700 group-hover:text-gray-900">
                {opt.label}
              </span>
            </label>
          ))}
        </div>
      </fieldset>

      {/* Category */}
      <div>
        <p className="mb-2 text-xs font-semibold uppercase tracking-wider text-muted-foreground">
          Category
        </p>
        {categoriesLoading ? (
          <div className="space-y-1.5">
            {[1, 2, 3, 4].map((i) => (
              <Skeleton key={i} className="h-7 w-full" />
            ))}
          </div>
        ) : (
          <CategoryTree
            categories={categories}
            selected={filters.categoryId}
            onSelect={(id) => onFiltersChange({ ...filters, categoryId: id, })}
          />
        )}
      </div>

      {/* Price range */}
      <fieldset>
        <legend className="mb-2 text-xs font-semibold uppercase tracking-wider text-muted-foreground">
          Price range
        </legend>
        <div className="flex items-center gap-2">
          <div className="flex-1">
            <Label htmlFor="min-price" className="sr-only">
              Minimum price
            </Label>
            <Input
              id="min-price"
              type="number"
              min={0}
              placeholder="Min"
              value={minInput}
              onChange={(e) => setMinInput(e.target.value)}
              onKeyDown={handlePriceKeyDown}
              onBlur={handlePriceApply}
              className="h-8 text-sm"
            />
          </div>
          <span className="text-muted-foreground text-sm">–</span>
          <div className="flex-1">
            <Label htmlFor="max-price" className="sr-only">
              Maximum price
            </Label>
            <Input
              id="max-price"
              type="number"
              min={0}
              placeholder="Max"
              value={maxInput}
              onChange={(e) => setMaxInput(e.target.value)}
              onKeyDown={handlePriceKeyDown}
              onBlur={handlePriceApply}
              className="h-8 text-sm"
            />
          </div>
        </div>
      </fieldset>

      {/* Brand */}
      <div>
        <Label
          htmlFor="brand-filter"
          className="mb-2 block text-xs font-semibold uppercase tracking-wider text-muted-foreground"
        >
          Brand
        </Label>
        <Input
          id="brand-filter"
          type="text"
          placeholder="Filter by brand…"
          value={filters.brand ?? ''}
          onChange={(e) =>
            onFiltersChange({ ...filters, brand: e.target.value || undefined })
          }
          className="h-8 text-sm"
        />
      </div>
    </div>
  );

  // Mobile: slide-in drawer
  return (
    <>
      {/* Mobile overlay */}
      {isOpen && (
        <div
          className="fixed inset-0 z-30 bg-black/30 md:hidden"
          onClick={onClose}
          aria-hidden="true"
        />
      )}

      {/* Sidebar panel */}
      <aside
        className={cn(
          'fixed inset-y-0 left-0 z-40 w-72 overflow-y-auto bg-white p-5 shadow-xl transition-transform duration-300 md:static md:z-auto md:block md:w-64 md:translate-x-0 md:rounded-xl md:border md:border-gray-100 md:shadow-none md:p-5',
          isOpen ? 'translate-x-0' : '-translate-x-full'
        )}
        aria-label="Product filters"
      >
        {content}
      </aside>
    </>
  );
}
