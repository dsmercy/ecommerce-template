import { useState, useRef, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { ShoppingCart, Search, LogOut, Package, Settings, Shield, Menu, X, ChevronDown } from 'lucide-react';
import { useMutation, useQuery } from '@tanstack/react-query';
import { toast } from 'sonner';

import { useAuthStore } from '@stores/authStore';
import { revokeToken } from '@api/endpoints/auth';
import { logger } from '@lib/logger';
import api from '@api/axios';
import { Button } from '@components/ui/button';
import { Input } from '@components/ui/input';
import { Separator } from '@components/ui/separator';
import { cn } from '@lib/utils';
import type { ApiResponse, CartResponse } from '@types/api';

// ─── Cart count query ─────────────────────────────────────────────────────────

function useCartItemCount() {
  const accessToken = useAuthStore((s) => s.accessToken);
  return useQuery({
    queryKey: ['cart'],
    queryFn: async () => {
      const { data } = await api.get<ApiResponse<CartResponse>>('/api/cart');
      return data.data;
    },
    enabled: !!accessToken,
    staleTime: 1000 * 60 * 2,
    select: (cart) => cart?.items.reduce((sum, item) => sum + item.quantity, 0) ?? 0,
  });
}

// ─── Header ───────────────────────────────────────────────────────────────────

export function Header() {
  const navigate = useNavigate();
  const { user, accessToken, logout } = useAuthStore();
  const [searchQuery, setSearchQuery] = useState('');
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const userMenuRef = useRef<HTMLDivElement>(null);
  const { data: cartCount = 0 } = useCartItemCount();

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (userMenuRef.current && !userMenuRef.current.contains(e.target as Node)) {
        setUserMenuOpen(false);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const logoutMutation = useMutation({
    mutationFn: async () => {
      const rt = localStorage.getItem('rt');
      if (rt) await revokeToken({ refreshToken: rt });
    },
    onSettled: () => {
      logout();
      logger.info('User logged out');
      toast.success('You have been signed out.');
      navigate('/login', { replace: true });
    },
  });

  function handleSearch(e: React.FormEvent) {
    e.preventDefault();
    const q = searchQuery.trim();
    if (q.length >= 2) {
      navigate(`/search?q=${encodeURIComponent(q)}`);
      setSearchQuery('');
      setMobileMenuOpen(false);
    }
  }

  return (
    <header className="sticky top-0 z-40 w-full border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
      <div className="mx-auto flex h-16 max-w-screen-xl items-center gap-4 px-4">

        {/* Logo */}
        <Link to="/" className="flex-shrink-0 text-xl font-bold text-primary">
          ShopApp
        </Link>

        {/* Search bar — desktop */}
        <form onSubmit={handleSearch} className="hidden flex-1 max-w-xl md:flex" role="search">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" aria-hidden="true" />
            <Input
              type="search"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Search products…"
              aria-label="Search products"
              className="pl-9 rounded-full bg-muted border-0 focus-visible:ring-primary"
            />
          </div>
        </form>

        <div className="flex-1 md:hidden" />

        {/* Right actions */}
        <div className="flex items-center gap-1">

          {/* Cart */}
          <Button
            variant="ghost"
            size="icon"
            asChild
            className="relative"
            aria-label={`Cart${cartCount > 0 ? `, ${cartCount} items` : ''}`}
          >
            <Link to={accessToken ? '/cart' : '/login?next=/cart'}>
              <ShoppingCart className="h-5 w-5" aria-hidden="true" />
              {cartCount > 0 && (
                <span
                  aria-hidden="true"
                  className="absolute -right-0.5 -top-0.5 flex h-5 w-5 items-center justify-center rounded-full bg-primary text-[10px] font-bold text-primary-foreground"
                >
                  {cartCount > 99 ? '99+' : cartCount}
                </span>
              )}
            </Link>
          </Button>

          {/* User menu — authenticated */}
          {accessToken && user ? (
            <div className="relative" ref={userMenuRef}>
              <button
                onClick={() => setUserMenuOpen((o) => !o)}
                aria-haspopup="true"
                aria-expanded={userMenuOpen}
                aria-label="User menu"
                className="flex items-center gap-1.5 rounded-full pl-1 pr-2 py-1 text-sm font-medium hover:bg-accent transition-colors focus:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              >
                <span className="flex h-8 w-8 items-center justify-center rounded-full bg-primary text-primary-foreground text-sm font-semibold">
                  {user.name.charAt(0).toUpperCase()}
                </span>
                <span className="hidden sm:block max-w-[100px] truncate">{user.name.split(' ')[0]}</span>
                <ChevronDown className={cn('h-3 w-3 text-muted-foreground transition-transform', userMenuOpen && 'rotate-180')} aria-hidden="true" />
              </button>

              {userMenuOpen && (
                <div
                  role="menu"
                  className="absolute right-0 mt-2 w-56 origin-top-right rounded-xl border bg-popover text-popover-foreground shadow-lg ring-1 ring-black/5 focus:outline-none animate-in fade-in-0 zoom-in-95"
                >
                  <div className="px-4 py-3">
                    <p className="truncate text-sm font-medium">{user.name}</p>
                    <p className="truncate text-xs text-muted-foreground">{user.email}</p>
                  </div>
                  <Separator />
                  <div className="py-1">
                    <MenuLink to="/orders" icon={<Package className="h-4 w-4" />} label="My Orders" onClick={() => setUserMenuOpen(false)} />
                    <MenuLink to="/profile" icon={<Settings className="h-4 w-4" />} label="Profile" onClick={() => setUserMenuOpen(false)} />
                    {user.role === 'ADMIN' && (
                      <MenuLink to="/admin" icon={<Shield className="h-4 w-4" />} label="Admin Panel" onClick={() => setUserMenuOpen(false)} />
                    )}
                  </div>
                  <Separator />
                  <div className="py-1">
                    <button
                      role="menuitem"
                      onClick={() => { setUserMenuOpen(false); logoutMutation.mutate(); }}
                      disabled={logoutMutation.isPending}
                      className="flex w-full items-center gap-3 px-4 py-2 text-sm text-destructive hover:bg-destructive/10 transition-colors disabled:opacity-50"
                    >
                      <LogOut className="h-4 w-4" aria-hidden="true" />
                      {logoutMutation.isPending ? 'Signing out…' : 'Sign out'}
                    </button>
                  </div>
                </div>
              )}
            </div>
          ) : (
            <Button variant="outline" size="sm" asChild>
              <Link to="/login">Sign in</Link>
            </Button>
          )}

          {/* Mobile hamburger */}
          <Button
            variant="ghost"
            size="icon"
            className="md:hidden"
            aria-label={mobileMenuOpen ? 'Close menu' : 'Open menu'}
            onClick={() => setMobileMenuOpen((o) => !o)}
          >
            {mobileMenuOpen ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
          </Button>
        </div>
      </div>

      {/* Mobile search */}
      {mobileMenuOpen && (
        <div className="border-t bg-background px-4 pb-4 pt-3 md:hidden">
          <form onSubmit={handleSearch} role="search" className="flex gap-2">
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" aria-hidden="true" />
              <Input
                type="search"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Search products…"
                className="pl-9"
              />
            </div>
            <Button type="submit" size="sm">Search</Button>
          </form>
        </div>
      )}
    </header>
  );
}

function MenuLink({ to, icon, label, onClick }: { to: string; icon: React.ReactNode; label: string; onClick: () => void; }) {
  return (
    <Link
      to={to}
      role="menuitem"
      onClick={onClick}
      className="flex items-center gap-3 px-4 py-2 text-sm hover:bg-accent transition-colors"
    >
      <span className="text-muted-foreground" aria-hidden="true">{icon}</span>
      {label}
    </Link>
  );
}
