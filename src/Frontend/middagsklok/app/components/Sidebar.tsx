"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useState, type CSSProperties } from "react";

type IconProps = {
  className?: string;
};

type MenuItem = {
  label: string;
  href?: string;
  icon: (props: IconProps) => JSX.Element;
};

const menuItems: MenuItem[] = [
  { label: "Dashboard", href: "/", icon: HomeIcon },
  { label: "Dishes", href: "/dishes", icon: DishIcon },
  { label: "Weekly Planner", icon: CalendarIcon },
  { label: "Shopping List", icon: CartIcon },
  { label: "Recipes", icon: BookIcon },
  { label: "Settings", icon: SettingsIcon },
  { label: "Ingredients", icon: LeafIcon },
];

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
    <aside
      className={`menu-shell flex min-h-[calc(100vh-3rem)] shrink-0 flex-col rounded-[28px] border border-[#e2e8dc] bg-[#fbfcf7]/90 shadow-[0_18px_40px_-28px_rgba(20,45,28,0.35)] backdrop-blur transition-[width] duration-300 ${
        collapsed ? "w-20" : "w-72"
      }`}
    >
      <div className="flex items-center justify-between gap-3 px-4 pt-5">
        <div className="flex items-center gap-3">
          <div className="grid h-11 w-11 place-items-center rounded-2xl bg-[#2f6b4f] text-white shadow-[0_8px_18px_-10px_rgba(32,78,54,0.7)]">
            <PanIcon className="h-5 w-5" />
          </div>
          <div
            className={`overflow-hidden transition-all duration-300 ${
              collapsed ? "max-w-0 opacity-0" : "max-w-[180px] opacity-100"
            }`}
          >
            <div className="text-base font-semibold text-[#1c2b22]">
              Meal Planner
            </div>
            <div className="text-[11px] font-semibold uppercase tracking-[0.26em] text-[#7b8a7f]">
              Plan & Cook
            </div>
          </div>
        </div>
        <button
          type="button"
          aria-label="Toggle menu"
          aria-expanded={!collapsed}
          aria-controls="primary-navigation"
          onClick={() => setCollapsed((current) => !current)}
          className="grid h-10 w-10 place-items-center rounded-full border border-[#e2e8dc] bg-white/70 text-[#2f6b4f] transition hover:bg-white"
        >
          <HamburgerIcon className="h-5 w-5" />
        </button>
      </div>

      <nav
        id="primary-navigation"
        aria-label="Primary"
        className="mt-6 flex-1 px-3"
      >
        <ul className="space-y-1">
          {menuItems.map((item, index) => {
            const isActive = item.href
              ? item.href === "/"
                ? pathname === "/"
                : pathname.startsWith(item.href)
              : false;
            const Icon = item.icon;
            const itemStyle = {
              "--delay": `${index * 70}ms`,
            } as CSSProperties;
            const content = (
              <>
                <span
                  className={`grid h-9 w-9 place-items-center rounded-xl transition ${
                    isActive
                      ? "bg-white/15 text-white"
                      : "bg-[#f3f6ef] text-[#2f6b4f] group-hover:bg-white"
                  }`}
                >
                  <Icon className="h-4 w-4" />
                </span>
                <span
                  className={`overflow-hidden whitespace-nowrap transition-all duration-300 ${
                    collapsed ? "max-w-0 opacity-0" : "max-w-[180px] opacity-100"
                  }`}
                >
                  {item.label}
                </span>
              </>
            );

            const baseClasses = `group flex w-full items-center rounded-2xl px-3 py-2.5 text-sm font-semibold transition ${
              collapsed ? "justify-center gap-0 px-2" : "justify-start gap-3"
            } ${
              isActive
                ? "bg-[#2f6b4f] text-white shadow-[0_12px_22px_-18px_rgba(25,68,45,0.8)]"
                : "text-[#3f4f45] hover:bg-[#eef3ea]"
            }`;

            return (
              <li key={item.label} className="menu-item" style={itemStyle}>
                {item.href ? (
                  <Link
                    href={item.href}
                    aria-current={isActive ? "page" : undefined}
                    className={baseClasses}
                  >
                    {content}
                  </Link>
                ) : (
                  <button type="button" className={baseClasses}>
                    {content}
                  </button>
                )}
              </li>
            );
          })}
        </ul>
      </nav>

      <div className="mt-auto px-3 pb-5">
        <div
          className={`rounded-2xl border border-[#dfe7d7] bg-[#e9f3e7] px-4 py-3 text-[#2d4a35] transition-all duration-300 ${
            collapsed ? "flex items-center justify-center" : ""
          }`}
        >
          <div
            className={`flex items-center ${collapsed ? "gap-0" : "gap-3"}`}
          >
            <span className="grid h-9 w-9 place-items-center rounded-xl bg-white/70 text-[#2f6b4f]">
              <SparkIcon className="h-4 w-4" />
            </span>
            <div
              className={`overflow-hidden transition-all duration-300 ${
                collapsed ? "max-w-0 opacity-0" : "max-w-[180px] opacity-100"
              }`}
            >
              <div className="text-[11px] font-semibold uppercase tracking-[0.22em]">
                Quick Stats
              </div>
              <div className="text-sm font-medium text-[#3a5a44]">
                24 dishes, 7 planned meals
              </div>
            </div>
          </div>
        </div>
      </div>
    </aside>
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

function HomeIcon({ className }: IconProps) {
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
      <path d="m4.5 11.5 7.5-6 7.5 6" />
      <path d="M7.5 10.5v8h9v-8" />
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

function SparkIcon({ className }: IconProps) {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
    >
      <path d="m12 4 1.7 4.3L18 10l-4.3 1.7L12 16l-1.7-4.3L6 10l4.3-1.7L12 4Z" />
    </svg>
  );
}
