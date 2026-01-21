'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { cn } from '@/lib/utils';

interface AppLayoutProps {
  children: React.ReactNode;
}

export default function AppLayout({ children }: AppLayoutProps) {
  const pathname = usePathname();

  const links = [
    { href: '/', label: 'Home' },
    { href: '/dishes', label: 'Dishes' },
    { href: '/weekly-plan', label: 'Weekly Plan' },
    { href: '/shopping-list', label: 'Shopping List' },
  ];

  return (
    <div className="min-h-screen bg-background">
      <header className="sticky top-0 z-50 w-full border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <div className="container flex h-16 items-center">
          <div className="mr-8 flex items-center">
            <Link href="/" className="flex items-center space-x-2">
              <span className="text-xl font-bold">Middagsklok</span>
            </Link>
          </div>
          <nav className="flex items-center gap-6 text-sm">
            {links.map((link) => (
              <Link
                key={link.href}
                href={link.href}
                className={cn(
                  'transition-colors hover:text-foreground/80',
                  pathname === link.href
                    ? 'text-foreground font-medium'
                    : 'text-foreground/60'
                )}
              >
                {link.label}
              </Link>
            ))}
          </nav>
        </div>
      </header>
      <main className="container mx-auto py-8 px-4 max-w-7xl">
        {children}
      </main>
    </div>
  );
}
