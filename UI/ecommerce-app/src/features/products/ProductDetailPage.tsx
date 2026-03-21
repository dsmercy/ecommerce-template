import { useState, useMemo, useCallback } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ChevronLeft, Minus, Plus, ShoppingCart, ImageOff, Loader2 } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@components/ui/button';
import { Badge } from '@components/ui/badge';
import { Skeleton } from '@components/ui/skeleton';
import { cn, formatPrice } from '@lib/utils';
import { getProductById, getInventoryByVariant } from '@api/endpoints/products';
import { useAuthStore } from '@stores/authStore';
import api from '@api/axios';
import type { Variant, ApiResponse, CartResponse } from '@types/api';
import DOMPurify from 'dompurify';

// ─── Add to cart mutation ─────────────────────────────────────────────────────

function useAddToCart() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ variantId, quantity }: { variantId: number; quantity: number }) => {
      const { data } = await api.post<ApiResponse<CartResponse>>('/api/cart/items', {
        variantId,
        quantity,
      });
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['cart'] });
      toast.success('Added to cart!');
    },
  });
}

// ─── VariantSelector ──────────────────────────────────────────────────────────

function VariantSelector({
  variants,
  selectedVariantId,
  onSelect,
}: {
  variants: Variant[];
  selectedVariantId: number | null;
  onSelect: (id: number) => void;
}) {
  const colors = [...new Set(variants.map((v) => v.color).filter(Boolean))] as string[];
  const sizes = [...new Set(variants.map((v) => v.size).filter(Boolean))] as string[];

  const [selectedColor, setSelectedColor] = useState<string | null>(
    () => variants.find((v) => v.id === selectedVariantId)?.color ?? colors[0] ?? null
  );
  const [selectedSize, setSelectedSize] = useState<string | null>(
    () => variants.find((v) => v.id === selectedVariantId)?.size ?? sizes[0] ?? null
  );

  // Resolve the variant that matches the selection
  useMemo(() => {
    if (!colors.length && !sizes.length) {
      if (variants[0]) onSelect(variants[0].id);
      return;
    }
    const matched = variants.find(
      (v) =>
        (colors.length === 0 || v.color === selectedColor) &&
        (sizes.length === 0 || v.size === selectedSize)
    );
    if (matched) onSelect(matched.id);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedColor, selectedSize]);

  return (
    <div className="space-y-4">
      {colors.length > 0 && (
        <fieldset>
          <legend className="mb-2 text-sm font-semibold text-gray-700">
            Color:{' '}
            <span className="font-normal text-muted-foreground">{selectedColor}</span>
          </legend>
          <div className="flex flex-wrap gap-2" role="group">
            {colors.map((color) => (
              <button
                key={color}
                onClick={() => setSelectedColor(color)}
                className={cn(
                  'h-9 min-w-[60px] rounded-lg border px-3 text-sm font-medium transition-all',
                  selectedColor === color
                    ? 'border-primary bg-primary/10 text-primary ring-2 ring-primary/30'
                    : 'border-gray-200 text-gray-700 hover:border-gray-400'
                )}
                aria-pressed={selectedColor === color}
              >
                {color}
              </button>
            ))}
          </div>
        </fieldset>
      )}

      {sizes.length > 0 && (
        <fieldset>
          <legend className="mb-2 text-sm font-semibold text-gray-700">Size</legend>
          <div className="flex flex-wrap gap-2" role="group">
            {sizes.map((size) => {
              const available = variants.some(
                (v) =>
                  v.size === size &&
                  (colors.length === 0 || v.color === selectedColor) &&
                  v.availableQuantity > 0
              );
              return (
                <button
                  key={size}
                  onClick={() => setSelectedSize(size)}
                  disabled={!available}
                  className={cn(
                    'h-9 min-w-[48px] rounded-lg border px-3 text-sm font-medium transition-all',
                    selectedSize === size
                      ? 'border-primary bg-primary/10 text-primary ring-2 ring-primary/30'
                      : 'border-gray-200 text-gray-700 hover:border-gray-400',
                    !available && 'cursor-not-allowed opacity-40 line-through'
                  )}
                  aria-pressed={selectedSize === size}
                  aria-disabled={!available}
                >
                  {size}
                </button>
              );
            })}
          </div>
        </fieldset>
      )}
    </div>
  );
}

// ─── ProductDetailPage ────────────────────────────────────────────────────────

export default function ProductDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const accessToken = useAuthStore((s) => s.accessToken);
  const productId = Number(id);

  const [selectedVariantId, setSelectedVariantId] = useState<number | null>(null);
  const [quantity, setQuantity] = useState(1);
  const [activeImage, setActiveImage] = useState<string | null>(null);

  const addToCart = useAddToCart();

  const { data: productRes, isPending, isError } = useQuery({
    queryKey: ['product', productId],
    queryFn: () => getProductById(productId),
    enabled: !isNaN(productId),
  });

  const product = productRes?.data;

  const { data: inventoryRes } = useQuery({
    queryKey: ['inventory', 'variant', selectedVariantId],
    queryFn: () => getInventoryByVariant(selectedVariantId!),
    enabled: selectedVariantId != null,
    staleTime: 1000 * 30,
  });

  const inventory = inventoryRes?.data;

  const selectedVariant = useMemo(
    () => product?.variants.find((v) => v.id === selectedVariantId) ?? null,
    [product, selectedVariantId]
  );

  const displayPrice = useMemo(() => {
    if (selectedVariant?.price != null) return selectedVariant.price;
    return product?.basePrice ?? null;
  }, [selectedVariant, product]);

  const availableQty = inventory?.availableQuantity ?? selectedVariant?.availableQuantity ?? 0;
  const isOutOfStock = selectedVariant != null && availableQty === 0;
  const hasVariants = (product?.variants?.length ?? 0) > 0;
  const canAddToCart = !isOutOfStock && (!hasVariants || selectedVariantId != null);

  const primaryImageUrl =
    activeImage ??
    product?.images.find((i) => i.isPrimary)?.imageUrl ??
    product?.images[0]?.imageUrl ??
    null;

  const handleAddToCart = useCallback(() => {
    if (!accessToken) {
      navigate(`/login?next=/products/${productId}`);
      return;
    }
    if (!selectedVariantId && hasVariants) {
      toast.error('Please select a variant first.');
      return;
    }
    const variantId = selectedVariantId ?? product?.variants[0]?.id;
    if (!variantId) return;
    addToCart.mutate({ variantId, quantity });
  }, [accessToken, selectedVariantId, hasVariants, product, quantity, addToCart, navigate, productId]);

  // ── Loading ──
  if (isPending) return <ProductDetailSkeleton />;

  // ── Error ──
  if (isError || !product) {
    return (
      <div className="mx-auto max-w-screen-xl px-4 py-16 text-center">
        <p className="text-muted-foreground">Product not found.</p>
        <Button variant="outline" className="mt-4" onClick={() => navigate('/products')}>
          Back to products
        </Button>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-screen-xl px-4 py-8 md:px-8">
      {/* Breadcrumb */}
      <nav aria-label="Breadcrumb" className="mb-6">
        <ol className="flex items-center gap-1.5 text-sm text-muted-foreground">
          <li>
            <Link to="/" className="hover:text-foreground transition-colors">
              Home
            </Link>
          </li>
          <li aria-hidden="true">/</li>
          <li>
            <Link to="/products" className="hover:text-foreground transition-colors">
              Products
            </Link>
          </li>
          <li aria-hidden="true">/</li>
          <li className="truncate font-medium text-foreground" aria-current="page">
            {product.name}
          </li>
        </ol>
      </nav>

      <div className="grid grid-cols-1 gap-8 lg:grid-cols-2 lg:gap-12">
        {/* ── Image gallery ── */}
        <div className="space-y-3">
          {/* Main image */}
          <div className="aspect-square overflow-hidden rounded-2xl border bg-gray-50">
            {primaryImageUrl ? (
              <img
                src={primaryImageUrl}
                alt={product.name}
                width={800}
                height={800}
                loading="eager"
                className="h-full w-full object-cover"
              />
            ) : (
              <div className="flex h-full w-full items-center justify-center">
                <ImageOff className="h-16 w-16 text-gray-300" aria-hidden="true" />
              </div>
            )}
          </div>

          {/* Thumbnail strip */}
          {product.images.length > 1 && (
            <div className="flex gap-2 overflow-x-auto pb-1" role="list" aria-label="Product images">
              {product.images.map((img) => (
                <button
                  key={img.id}
                  onClick={() => setActiveImage(img.imageUrl)}
                  role="listitem"
                  className={cn(
                    'h-16 w-16 flex-shrink-0 overflow-hidden rounded-lg border-2 transition-all',
                    (activeImage ?? product.images.find((i) => i.isPrimary)?.imageUrl ?? product.images[0]?.imageUrl) === img.imageUrl
                      ? 'border-primary'
                      : 'border-transparent hover:border-gray-300'
                  )}
                  aria-label={`View image ${img.id}`}
                >
                  <img
                    src={img.imageUrl}
                    alt=""
                    width={64}
                    height={64}
                    loading="lazy"
                    className="h-full w-full object-cover"
                  />
                </button>
              ))}
            </div>
          )}
        </div>

        {/* ── Product info ── */}
        <div className="flex flex-col gap-5">
          {/* Brand + name */}
          {product.brand && (
            <p className="text-sm font-medium uppercase tracking-wider text-muted-foreground">
              {product.brand}
            </p>
          )}
          <h1 className="text-3xl font-bold text-gray-900 leading-tight">{product.name}</h1>

          {/* Category */}
          {product.categoryName && (
            <Badge variant="secondary" className="self-start">
              {product.categoryName}
            </Badge>
          )}

          {/* Price */}
          <div className="flex items-baseline gap-3">
            <span className="text-3xl font-bold text-gray-900">
              {displayPrice != null ? formatPrice(displayPrice) : 'See options'}
            </span>
          </div>

          {/* Live inventory badge */}
          {selectedVariant && (
            <div className="flex items-center gap-2">
              {isOutOfStock ? (
                <Badge variant="destructive">Out of stock</Badge>
              ) : (
                <Badge variant="success">
                  {availableQty <= 10 ? `Only ${availableQty} left` : 'In stock'}
                </Badge>
              )}
            </div>
          )}

          {/* Variant selector */}
          {hasVariants && (
            <VariantSelector
              variants={product.variants}
              selectedVariantId={selectedVariantId}
              onSelect={setSelectedVariantId}
            />
          )}

          {/* Quantity selector */}
          <div className="flex items-center gap-4">
            <span className="text-sm font-medium text-gray-700">Qty</span>
            <div className="flex items-center rounded-lg border">
              <button
                onClick={() => setQuantity((q) => Math.max(1, q - 1))}
                disabled={quantity <= 1}
                className="flex h-9 w-9 items-center justify-center rounded-l-lg hover:bg-gray-50 disabled:opacity-40"
                aria-label="Decrease quantity"
              >
                <Minus className="h-3.5 w-3.5" aria-hidden="true" />
              </button>
              <span className="w-10 text-center text-sm font-medium" aria-live="polite">
                {quantity}
              </span>
              <button
                onClick={() =>
                  setQuantity((q) => Math.min(10, availableQty > 0 ? Math.min(q + 1, availableQty) : q + 1))
                }
                disabled={quantity >= 10 || (availableQty > 0 && quantity >= availableQty)}
                className="flex h-9 w-9 items-center justify-center rounded-r-lg hover:bg-gray-50 disabled:opacity-40"
                aria-label="Increase quantity"
              >
                <Plus className="h-3.5 w-3.5" aria-hidden="true" />
              </button>
            </div>
          </div>

          {/* Add to cart */}
          <Button
            size="lg"
            onClick={handleAddToCart}
            disabled={!canAddToCart || addToCart.isPending}
            className="w-full gap-2"
            aria-disabled={!canAddToCart}
          >
            {addToCart.isPending ? (
              <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
            ) : (
              <ShoppingCart className="h-4 w-4" aria-hidden="true" />
            )}
            {isOutOfStock
              ? 'Out of stock'
              : hasVariants && !selectedVariantId
              ? 'Select options'
              : addToCart.isPending
              ? 'Adding…'
              : 'Add to cart'}
          </Button>

          {/* Description */}
          {product.description && (
            <div className="border-t pt-5">
              <h2 className="mb-3 font-semibold text-gray-900">Description</h2>
              <div
                className="prose prose-sm max-w-none text-gray-600"
                dangerouslySetInnerHTML={{
                  __html: DOMPurify.sanitize(product.description, {
                    USE_PROFILES: { html: true },
                  }),
                }}
              />
            </div>
          )}
        </div>
      </div>

      {/* Back link */}
      <div className="mt-10 border-t pt-6">
        <Button
          variant="ghost"
          size="sm"
          onClick={() => navigate(-1)}
          className="gap-1.5 text-muted-foreground"
        >
          <ChevronLeft className="h-4 w-4" aria-hidden="true" />
          Back
        </Button>
      </div>
    </div>
  );
}

// ─── Skeleton ─────────────────────────────────────────────────────────────────

function ProductDetailSkeleton() {
  return (
    <div className="mx-auto max-w-screen-xl px-4 py-8 md:px-8">
      <Skeleton className="mb-6 h-4 w-48" />
      <div className="grid grid-cols-1 gap-8 lg:grid-cols-2">
        <Skeleton className="aspect-square w-full rounded-2xl" />
        <div className="space-y-4">
          <Skeleton className="h-4 w-24" />
          <Skeleton className="h-9 w-3/4" />
          <Skeleton className="h-8 w-32" />
          <Skeleton className="h-6 w-24" />
          <div className="flex gap-2">
            {[1, 2, 3].map((i) => (
              <Skeleton key={i} className="h-9 w-16 rounded-lg" />
            ))}
          </div>
          <Skeleton className="h-11 w-full rounded-md" />
          <div className="border-t pt-4 space-y-2">
            <Skeleton className="h-4 w-full" />
            <Skeleton className="h-4 w-5/6" />
            <Skeleton className="h-4 w-4/6" />
          </div>
        </div>
      </div>
    </div>
  );
}
