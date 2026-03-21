import { NavLink, Outlet } from 'react-router-dom';
import {
  LayoutDashboard,
  Package,
  ShoppingBag,
  Warehouse,
  Tag,
  FolderTree,
} from 'lucide-react';
import { cn } from '@lib/utils';

// ─── Nav items ────────────────────────────────────────────────────────────────

const NAV_ITEMS = [
  { to: '/admin',             icon: LayoutDashboard, label: 'Dashboard',  end: true },
  { to: '/admin/products',    icon: Package,         label: 'Products'        },
  { to: '/admin/orders',      icon: ShoppingBag,     label: 'Orders'          },
  { to: '/admin/inventory',   icon: Warehouse,       label: 'Inventory'       },
  { to: '/admin/coupons',     icon: Tag,             label: 'Coupons'         },
  { to: '/admin/categories',  icon: FolderTree,      label: 'Categories'      },
] as const;

// ─── AdminLayout ──────────────────────────────────────────────────────────────

export function AdminLayout() {
  return (
    <div className="flex min-h-[calc(100vh-4rem)]">
      {/* ── Sidebar ── */}
      <aside className="hidden w-60 flex-shrink-0 border-r border-gray-200 bg-gray-50 md:block">
        <div className="sticky top-16 pt-6 pb-8">
          <p className="mb-3 px-5 text-xs font-semibold uppercase tracking-widest text-gray-400">
            Admin
          </p>
          <nav aria-label="Admin navigation">
            <ul className="space-y-0.5 px-3">
              {NAV_ITEMS.map(({ to, icon: Icon, label, ...rest }) => (
                <li key={to}>
                  <NavLink
                    to={to}
                    end={'end' in rest ? rest.end : false}
                    className={({ isActive }) =>
                      cn(
                        'flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition-colors',
                        isActive
                          ? 'bg-violet-100 text-violet-700'
                          : 'text-gray-600 hover:bg-gray-100 hover:text-gray-900'
                      )
                    }
                  >
                    <Icon className="h-4 w-4 flex-shrink-0" aria-hidden="true" />
                    {label}
                  </NavLink>
                </li>
              ))}
            </ul>
          </nav>
        </div>
      </aside>

      {/* ── Page content ── */}
      <div className="flex-1 overflow-x-hidden">
        <div className="mx-auto max-w-screen-xl px-4 py-8 md:px-8">
          <Outlet />
        </div>
      </div>
    </div>
  );
}
