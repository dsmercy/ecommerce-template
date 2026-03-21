import { lazy, Suspense, type JSX } from 'react';
import { createBrowserRouter, Outlet } from 'react-router-dom';

import { PageSkeleton } from '@components/shared/PageSkeleton';
import { ErrorBoundaryPage } from '@components/shared/ErrorBoundaryPage';
import { RequireAuth } from '@features/auth/RequireAuth';
import { RequireAdmin } from '@features/auth/RequireAdmin';
import { RootLayout } from '@components/layout/RootLayout';
import { AdminLayout } from '@components/layout/AdminLayout';

// ─── Lazy page imports — one JS chunk per page ────────────────────────────────

// Auth
const LoginPage      = lazy(() => import('@features/auth/LoginPage'));
const RegisterPage   = lazy(() => import('@features/auth/RegisterPage'));

// Products / storefront (Phase 3 — stubbed for now)
const HomePage            = lazy(() => import('@features/products/HomePage'));
const ProductListPage     = lazy(() => import('@features/products/ProductListPage'));
const ProductDetailPage   = lazy(() => import('@features/products/ProductDetailPage'));
const SearchResultsPage   = lazy(() => import('@features/search/SearchResultsPage'));

// Cart & checkout (Phase 4 — stubbed)
const CartPage      = lazy(() => import('@features/cart/CartPage'));
const CheckoutPage  = lazy(() => import('@features/checkout/CheckoutPage'));

// Orders & profile (Phase 5 — stubbed)
const OrderHistoryPage = lazy(() => import('@features/orders/OrderHistoryPage'));
const OrderDetailPage  = lazy(() => import('@features/orders/OrderDetailPage'));
const ProfilePage      = lazy(() => import('@features/auth/ProfilePage'));

// Admin (Phase 6 — stubbed)
const AdminDashboard   = lazy(() => import('@features/admin/AdminDashboard'));
const AdminProductList = lazy(() => import('@features/admin/AdminProductListPage'));
const AdminProductForm = lazy(() => import('@features/admin/AdminProductFormPage'));
const AdminOrderList   = lazy(() => import('@features/admin/AdminOrderListPage'));
const AdminInventory   = lazy(() => import('@features/admin/AdminInventoryPage'));
const AdminCoupons     = lazy(() => import('@features/admin/AdminCouponPage'));
const AdminCategories  = lazy(() => import('@features/admin/AdminCategoryPage'));

// ─── Suspense wrapper helper ──────────────────────────────────────────────────

function wrap(Component: React.LazyExoticComponent<() => JSX.Element>) {
  return (
    <Suspense fallback={<PageSkeleton />}>
      <Component />
    </Suspense>
  );
}

// ─── Router ───────────────────────────────────────────────────────────────────

export const router = createBrowserRouter([
  {
    element: <RootLayout />,
    errorElement: <ErrorBoundaryPage />,
    children: [
      // ── Public routes ──
      { path: '/',                element: wrap(HomePage) },
      { path: '/products',        element: wrap(ProductListPage) },
      { path: '/products/:id',    element: wrap(ProductDetailPage) },
      { path: '/categories/:slug',element: wrap(ProductListPage) },
      { path: '/search',          element: wrap(SearchResultsPage) },
      { path: '/login',           element: wrap(LoginPage) },
      { path: '/register',        element: wrap(RegisterPage) },

      // ── Protected routes (must be authenticated) ──
      {
        element: (
          <RequireAuth>
            <Outlet />
          </RequireAuth>
        ),
        children: [
          { path: '/cart',        element: wrap(CartPage) },
          { path: '/checkout',    element: wrap(CheckoutPage) },
          { path: '/orders',      element: wrap(OrderHistoryPage) },
          { path: '/orders/:id',  element: wrap(OrderDetailPage) },
          { path: '/profile',     element: wrap(ProfilePage) },
        ],
      },

      // ── Admin routes (must be ADMIN role) ──
      {
        element: (
          <RequireAuth>
            <RequireAdmin>
              <AdminLayout />
            </RequireAdmin>
          </RequireAuth>
        ),
        children: [
          { path: '/admin',                    element: wrap(AdminDashboard) },
          { path: '/admin/products',           element: wrap(AdminProductList) },
          { path: '/admin/products/new',       element: wrap(AdminProductForm) },
          { path: '/admin/products/:id/edit',  element: wrap(AdminProductForm) },
          { path: '/admin/orders',             element: wrap(AdminOrderList) },
          { path: '/admin/inventory',          element: wrap(AdminInventory) },
          { path: '/admin/coupons',            element: wrap(AdminCoupons) },
          { path: '/admin/categories',         element: wrap(AdminCategories) },
        ],
      },
    ],
  },
]);
