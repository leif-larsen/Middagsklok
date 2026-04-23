"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { JSX, useEffect, useState, type CSSProperties } from "react";

type IconProps = {
  className?: string;
};

type MenuItem = {
  label: string;
  href: string;
  icon: (props: IconProps) => JSX.Element;
};

const menuItems: MenuItem[] = [
  { label: "Retter", href: "/dishes", icon: DishIcon },
  { label: "Ukesplan", href: "/weekly-planner", icon: CalendarIcon },
  { label: "Handleliste", href: "/shopping-list", icon: CartIcon },
  { label: "Oppskrifter", href: "/recipes", icon: BookIcon },
  { label: "Innstillinger", href: "/settings", icon: SettingsIcon },
  { label: "Ingredienser", href: "/ingredients", icon: LeafIcon },
];

const isItemActive = (href: string, pathname: string) =>
  href === "/" ? pathname === "/" : pathname.startsWith(href);

export default function Sidebar() {
  const pathname = usePathname();
  const [collapsed, setCollapsed] = useState(false);

  useEffect(() => {
    const mediaQuery = window.matchMedia("(min-width: 640px)");

    const handleChange = (event: MediaQueryListEvent | MediaQueryList) => {
      setCollapsed(!event.matches);
    };

    handleChange(mediaQuery);
    mediaQuery.addEventListener("change", handleChange);

    return () => {
      mediaQuery.removeEventListener("change", handleChange);
    };
  }, []);

  return (
    <>
      {/* Mobile: compact horizontal icon bar */}
      <nav
        className="flex w-full items-center justify-around rounded-[28px] border border-[#e2e8dc] bg-[#fbfcf7]/90 px-2 py-3 shadow-[0_18px_40px_-28px_rgba(20,45,28,0.35)] sm:hidden"
        aria-label="Primary"
      >
        {menuItems.map((item) => {
          const isActive = isItemActive(item.href, pathname);
          const Icon = item.icon;
          return (
            <Link
              key={item.label}
              href={item.href}
              aria-label={item.label}
              aria-current={isActive ? "page" : undefined}
              className={`grid h-10 w-10 place-items-center rounded-xl transition ${
                isActive
                  ? "bg-[#2f6b4f] text-white shadow-[0_8px_16px_-10px_rgba(25,68,45,0.8)]"
                  : "bg-[#f3f6ef] text-[#2f6b4f] hover:bg-[#e4ede6]"
              }`}
            >
              <Icon className="h-4 w-4" />
            </Link>
          );
        })}
      </nav>

      {/* Desktop: collapsible vertical sidebar */}
      <aside
        className={`menu-shell hidden shrink-0 flex-col rounded-[28px] border border-[#e2e8dc] bg-[#fbfcf7]/90 shadow-[0_18px_40px_-28px_rgba(20,45,28,0.35)] backdrop-blur transition-[width] duration-300 sm:flex ${
          collapsed ? "w-20" : "w-72"
        }`}
        style={{ minHeight: "calc(100vh - 3rem)" }}
      >
        {collapsed ? (
          <div className="flex justify-center pt-5">
            <button
              type="button"
              aria-label="Utvid meny"
              aria-expanded={false}
              aria-controls="primary-navigation"
              onClick={() => setCollapsed(false)}
              className="grid h-10 w-10 place-items-center rounded-full border border-[#e2e8dc] bg-white/70 text-[#2f6b4f] transition hover:bg-white"
            >
              <HamburgerIcon className="h-5 w-5" />
            </button>
          </div>
        ) : (
          <div className="flex items-center justify-between gap-3 px-4 pt-5">
            <div className="flex items-center gap-3">
              <div className="grid h-11 w-11 shrink-0 place-items-center rounded-2xl bg-[#2f6b4f] text-white shadow-[0_8px_18px_-10px_rgba(32,78,54,0.7)]">
                <PanIcon className="h-5 w-5" />
              </div>
              <div>
                <div className="text-base font-semibold text-[#1c2b22]">
                  Middagsklok
                </div>
                <div className="text-[11px] font-semibold uppercase tracking-[0.26em] text-[#7b8a7f]">
                  Planlegg & lag mat
                </div>
              </div>
            </div>
            <button
              type="button"
              aria-label="Skjul meny"
              aria-expanded={true}
              aria-controls="primary-navigation"
              onClick={() => setCollapsed(true)}
              className="grid h-10 w-10 shrink-0 place-items-center rounded-full border border-[#e2e8dc] bg-white/70 text-[#2f6b4f] transition hover:bg-white"
            >
              <HamburgerIcon className="h-5 w-5" />
            </button>
          </div>
        )}

        <nav
          id="primary-navigation"
          aria-label="Primary"
          className="mt-6 flex-1 px-3"
        >
          <ul className="space-y-1">
            {menuItems.map((item, index) => {
              const isActive = isItemActive(item.href, pathname);
              const Icon = item.icon;
              const itemStyle = {
                "--delay": `${index * 70}ms`,
              } as CSSProperties;

              const content = (
                <>
                  <span
                    className={`grid h-9 w-9 shrink-0 place-items-center rounded-xl transition ${
                      isActive
                        ? "bg-white/15 text-white"
                        : "bg-[#f3f6ef] text-[#2f6b4f] group-hover:bg-white"
                    }`}
                  >
                    <Icon className="h-4 w-4" />
                  </span>
                  {!collapsed && (
                    <span className="whitespace-nowrap">{item.label}</span>
                  )}
                </>
              );

              const baseClasses = `group flex w-full items-center rounded-2xl py-2.5 text-sm font-semibold transition ${
                collapsed ? "justify-center px-2" : "justify-start gap-3 px-3"
              } ${
                isActive
                  ? "bg-[#2f6b4f] text-white shadow-[0_12px_22px_-18px_rgba(25,68,45,0.8)]"
                  : "text-[#3f4f45] hover:bg-[#eef3ea]"
              }`;

              return (
                <li key={item.label} className="menu-item" style={itemStyle}>
                  <Link
                    href={item.href}
                    aria-current={isActive ? "page" : undefined}
                    className={baseClasses}
                  >
                    {content}
                  </Link>
                </li>
              );
            })}
          </ul>
        </nav>
      </aside>
    </>
  );
}

function HamburgerIcon({ className }: IconProps) {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      className={className}
    >
      <path d="M4 7h16M4 12h16M4 17h16" />
    </svg>
  );
}

function DishIcon({ className }: IconProps) {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
    >
      <path d="M4.5 6h15" />
      <path d="M6.5 10h11" />
      <path d="M8.5 14h7" />
      <path d="M10 18h4" />
    </svg>
  );
}

function CalendarIcon({ className }: IconProps) {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
    >
      <rect x="4" y="5" width="16" height="15" rx="3" />
      <path d="M8 3v4M16 3v4M4 10h16" />
    </svg>
  );
}

function CartIcon({ className }: IconProps) {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
    >
      <circle cx="9" cy="19" r="1.5" />
      <circle cx="17" cy="19" r="1.5" />
      <path d="M3.5 5h2l2 10h10.5l2-7H7" />
    </svg>
  );
}

function BookIcon({ className }: IconProps) {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
    >
      <path d="M6 4.5h10a2 2 0 0 1 2 2v12" />
      <path d="M6 4.5a2 2 0 0 0-2 2v12a2 2 0 0 1 2-2h12" />
    </svg>
  );
}

function SettingsIcon({ className }: IconProps) {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
    >
      <path d="M12 8.5a3.5 3.5 0 1 1 0 7 3.5 3.5 0 0 1 0-7Z" />
      <path d="M5 12a7 7 0 0 1 .08-1l-1.9-1.48 2-3.46 2.3.7a7.2 7.2 0 0 1 1.74-1L9.6 3h4.8l.48 2.76a7.2 7.2 0 0 1 1.74 1l2.3-.7 2 3.46-1.9 1.48c.05.33.08.66.08 1s-.03.67-.08 1l1.9 1.48-2 3.46-2.3-.7a7.2 7.2 0 0 1-1.74 1L14.4 21H9.6l-.48-2.76a7.2 7.2 0 0 1-1.74-1l-2.3.7-2-3.46 1.9-1.48A7 7 0 0 1 5 12Z" />
    </svg>
  );
}

function LeafIcon({ className }: IconProps) {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
    >
      <path d="M5 18c6.5-1 9-5.5 14-12" />
      <path d="M6 8c3.5 0 6.5 3 6.5 6.5V19c-3.5 0-6.5-3-6.5-6.5V8Z" />
    </svg>
  );
}

function PanIcon({ className }: IconProps) {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
    >
      <path d="M5 12.5h10a4 4 0 0 1 0 8H7a2 2 0 0 1-2-2v-6Z" />
      <path d="M15 12.5h4.5a1.5 1.5 0 1 0 0-3H14" />
      <path d="M9 7.5h3" />
    </svg>
  );
}
