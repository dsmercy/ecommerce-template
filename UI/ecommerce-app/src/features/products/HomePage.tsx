import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { ArrowRight, Sparkles, TrendingUp, ShieldCheck } from 'lucide-react';
import { Button } from '@components/ui/button';
import { ProductCard } from '@components/shared/ProductCard';
import { ProductGridSkeleton } from '@components/shared/ProductCardSkeleton';
import { getProducts } from '@api/endpoints/products';

// ─── Feature highlights ───────────────────────────────────────────────────────

const FEATURES = [
  {
    icon: TrendingUp,
    title: 'Curated Selection',
    desc: 'Hand-picked products from top brands worldwide.',
  },
  {
    icon: ShieldCheck,
    title: 'Secure Shopping',
    desc: 'Your data is encrypted and your payments are safe.',
  },
  {
    icon: Sparkles,
    title: 'New Arrivals Daily',
    desc: 'Fresh inventory added every day — come back often.',
  },
];

// ─── HomePage ─────────────────────────────────────────────────────────────────

export default function HomePage() {
  const { data, isPending } = useQuery({
    queryKey: ['products', { sort: 'newest', page: 1, pageSize: 8 }],
    queryFn: () => getProducts({ sort: 'newest', page: 1, pageSize: 8 }),
  });

  const products = data?.data?.items ?? [];

  return (
    <div>
      {/* ── Hero ── */}
      <section className="relative overflow-hidden bg-gradient-to-br from-violet-600 via-purple-600 to-indigo-700 px-4 py-24 text-white md:py-32">
        {/* Decorative circles */}
        <div
          aria-hidden="true"
          className="pointer-events-none absolute -right-24 -top-24 h-96 w-96 rounded-full bg-white/5"
        />
        <div
          aria-hidden="true"
          className="pointer-events-none absolute -bottom-32 -left-16 h-72 w-72 rounded-full bg-white/5"
        />

        <div className="relative mx-auto max-w-screen-xl text-center">
          <p className="mb-4 inline-block rounded-full bg-white/10 px-4 py-1.5 text-xs font-semibold uppercase tracking-widest">
            New season arrivals
          </p>
          <h1 className="mx-auto mb-6 max-w-3xl text-4xl font-extrabold leading-tight tracking-tight sm:text-5xl md:text-6xl">
            Discover your next{' '}
            <span className="relative inline-block">
              <span className="relative z-10">favourite</span>
              <span
                aria-hidden="true"
                className="absolute -bottom-1 left-0 right-0 h-3 rounded-full bg-yellow-400/40"
              />
            </span>{' '}
            purchase
          </h1>
          <p className="mx-auto mb-10 max-w-xl text-lg text-white/80">
            Thousands of products. Exclusive deals. Delivered fast.
          </p>
          <div className="flex flex-col items-center gap-3 sm:flex-row sm:justify-center">
            <Button
              asChild
              size="lg"
              className="bg-white text-violet-700 hover:bg-white/90 font-semibold px-8"
            >
              <Link to="/products">Shop now</Link>
            </Button>
            <Button
              asChild
              size="lg"
              variant="outline"
              className="border-white/40 text-white hover:bg-white/10 hover:text-white"
            >
              <Link to="/search">Search products</Link>
            </Button>
          </div>
        </div>
      </section>

      {/* ── Feature highlights ── */}
      <section className="border-b bg-gray-50 px-4 py-10" aria-label="Why shop with us">
        <div className="mx-auto grid max-w-screen-xl grid-cols-1 gap-6 sm:grid-cols-3">
          {FEATURES.map(({ icon: Icon, title, desc }) => (
            <div key={title} className="flex items-start gap-4">
              <div className="flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-xl bg-violet-100">
                <Icon className="h-5 w-5 text-violet-600" aria-hidden="true" />
              </div>
              <div>
                <h3 className="font-semibold text-gray-900">{title}</h3>
                <p className="mt-0.5 text-sm text-muted-foreground">{desc}</p>
              </div>
            </div>
          ))}
        </div>
      </section>

      {/* ── New arrivals ── */}
      <section className="mx-auto max-w-screen-xl px-4 py-12 md:px-8 md:py-16">
        <div className="mb-6 flex items-center justify-between">
          <div>
            <h2 className="text-2xl font-bold text-gray-900">New Arrivals</h2>
            <p className="mt-1 text-sm text-muted-foreground">
              The latest products added to our catalog
            </p>
          </div>
          <Button asChild variant="ghost" size="sm" className="gap-1.5 text-primary">
            <Link to="/products">
              View all
              <ArrowRight className="h-4 w-4" aria-hidden="true" />
            </Link>
          </Button>
        </div>

        {isPending ? (
          <ProductGridSkeleton count={8} />
        ) : (
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
            {products.map((product) => (
              <ProductCard key={product.id} product={product} />
            ))}
          </div>
        )}

        <div className="mt-10 text-center">
          <Button asChild size="lg" variant="outline" className="px-10">
            <Link to="/products">Browse all products</Link>
          </Button>
        </div>
      </section>
    </div>
  );
}
