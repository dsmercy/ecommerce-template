import { Outlet } from 'react-router-dom';
import { Header } from './Header';

/**
 * Root layout wraps every public and protected page.
 * Contains the Header (with search bar, cart badge, and user menu)
 * and renders the matched child route via <Outlet />.
 */
export function RootLayout() {
  return (
    <div className="flex min-h-screen flex-col bg-white">
      <Header />
      <main className="flex-1">
        <Outlet />
      </main>
      <footer className="border-t border-gray-200 bg-gray-50 py-8 text-center text-sm text-gray-500">
        © {new Date().getFullYear()} ShopApp. All rights reserved.
      </footer>
    </div>
  );
}
