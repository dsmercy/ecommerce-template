import React from 'react';
import { Link } from 'react-router-dom';
import { ShoppingCart, ImageOff } from 'lucide-react';
import { Badge } from '@components/ui/badge';
import { Button } from '@components/ui/button';
import { cn, formatPrice } from '@lib/utils';
import type { ProductListItem } from '@types/api';

// ─── Props ────────────────────────────────────────────────────────────────────

interface ProductCardProps {
  product: ProductListItem;
  onAddToCart?: (product: ProductListItem) => void;
  isAddingToCart?: boolean;
}

// ─── ProductCard — React.memo to prevent re-renders in large lists ─────────────

export const ProductCard = React.memo(function ProductCard({
  product,
  onAddToCart,
  isAddingToCart = false,
}: ProductCardProps) {
  const isOutOfStock = false; // stock checked on detail page; list view shows availability via badge if needed

  return (
    <article className="group relative flex flex-col rounded-xl border border-gray-100 bg-white shadow-sm transition-all duration-200 hover:shadow-md hover:-translate-y-0.5 overflow-hidden">
      {/* Product image */}
      <Link
        to={`/products/${product.id}`}
        aria-label={`View ${product.name}`}
        className="block aspect-square overflow-hidden bg-gray-50"
        tabIndex={0}
      >
        {product.primaryImageUrl ? (
          <img
            src={product.primaryImageUrl}
            alt={product.name}
            width={400}
            height={400}
            loading="lazy"
            className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105"
          />
        ) : (
          <div className="flex h-full w-full items-center justify-center">
            <ImageOff className="h-10 w-10 text-gray-300" aria-hidden="true" />
          </div>
        )}

        {/* Out of stock overlay */}
        {isOutOfStock && (
          <div className="absolute inset-0 flex items-center justify-center bg-black/40">
            <span className="rounded-full bg-white px-3 py-1 text-xs font-semibold text-gray-700">
              Out of stock
            </span>
          </div>
        )}
      </Link>

      {/* Card body */}
      <div className="flex flex-1 flex-col gap-2 p-4">
        {/* Brand */}
        {product.brand && (
          <p className="text-xs font-medium uppercase tracking-wider text-muted-foreground">
            {product.brand}
          </p>
        )}

        {/* Name */}
        <Link
          to={`/products/${product.id}`}
          className="text-sm font-semibold text-gray-900 line-clamp-2 hover:text-primary transition-colors leading-snug"
          tabIndex={-1}
          aria-hidden="true"
        >
          {product.name}
        </Link>

        {/* Category */}
        {product.categoryName && (
          <Badge variant="secondary" className="self-start text-[10px] px-1.5 py-0.5">
            {product.categoryName}
          </Badge>
        )}

        {/* Price + CTA */}
        <div className="mt-auto flex items-center justify-between gap-2 pt-2">
          <span className="text-base font-bold text-gray-900">
            {product.basePrice != null ? formatPrice(product.basePrice) : 'See options'}
          </span>

          {onAddToCart && (
            <Button
              size="sm"
              variant="outline"
              disabled={isAddingToCart || isOutOfStock}
              onClick={(e) => {
                e.preventDefault();
                onAddToCart(product);
              }}
              aria-label={`Add ${product.name} to cart`}
              className={cn(
                'h-8 w-8 flex-shrink-0 rounded-full p-0 transition-all',
                'hover:bg-primary hover:text-primary-foreground hover:border-primary'
              )}
            >
              <ShoppingCart className="h-3.5 w-3.5" aria-hidden="true" />
            </Button>
          )}
        </div>
      </div>
    </article>
  );
});

ProductCard.displayName = 'ProductCard';
