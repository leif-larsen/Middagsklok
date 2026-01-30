"use client";

import { useEffect, useMemo, useState } from "react";
import { ApiError, apiClient } from "../../lib/api/client";
import type { ShoppingListResponse } from "../../lib/api/models/shopping-list";
import Sidebar from "../components/Sidebar";

type PlanOption = {
  startDate: string;
  label: string;
  subtitle: string;
};

const weekStartsOn = 1;
const anchorDate = new Date(2026, 0, 30);
const rangeFormatter = new Intl.DateTimeFormat("en-US", {
  month: "short",
  day: "numeric",
});

const addDays = (date: Date, amount: number) => {
  const next = new Date(date);
  next.setDate(date.getDate() + amount);

  return next;
};

const startOfWeek = (date: Date, weekStartsOnValue: number) => {
  const dayIndex = date.getDay();
  const diff = (dayIndex - weekStartsOnValue + 7) % 7;

  return addDays(date, -diff);
};

const formatDateKey = (date: Date) => {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");

  return `${year}-${month}-${day}`;
};

const buildPlanOptions = (): PlanOption[] => {
  const baseWeekStart = startOfWeek(anchorDate, weekStartsOn);
  const descriptors = [
    { offset: 0, subtitle: "Current week plan" },
    { offset: 1, subtitle: "Next week plan" },
    { offset: 2, subtitle: "Upcoming plan" },
  ];

  return descriptors.map(({ offset, subtitle }) => {
    const startDate = addDays(baseWeekStart, offset * 7);
    const endDate = addDays(startDate, 6);

    return {
      startDate: formatDateKey(startDate),
      label: `${rangeFormatter.format(startDate)} - ${rangeFormatter.format(endDate)}`,
      subtitle,
    };
  });
};

const planOptions = buildPlanOptions();

const categoryLabels: Record<string, string> = {
  DairyAndEggs: "Dairy & Eggs",
  PastaAndGrains: "Pasta & Grains",
  SpicesAndHerbs: "Spices & Herbs",
  OilsAndVinegars: "Oils & Vinegars",
  FrozenFoods: "Frozen Foods",
  CannedGoods: "Canned Goods",
};

const formatCategoryLabel = (value: string) => {
  if (categoryLabels[value]) {
    return categoryLabels[value];
  }

  return value.replace(/([a-z])([A-Z])/g, "$1 $2");
};

const unitLabels: Record<string, string> = {
  G: "g",
  Kg: "kg",
  Ml: "ml",
  L: "l",
  Pcs: "pcs",
};

const formatQuantity = (value: number) => {
  if (!Number.isFinite(value)) {
    return "0";
  }

  const rounded = Math.round(value * 100) / 100;
  const hasFraction = Math.abs(rounded % 1) > 0.0001;

  return hasFraction
    ? rounded.toFixed(2).replace(/\.?0+$/, "")
    : rounded.toFixed(0);
};

const formatUnit = (unit: string) => unitLabels[unit] ?? unit.toLowerCase();

const formatAmount = (amount: number, unit: string) =>
  `${formatQuantity(amount)} ${formatUnit(unit)}`.trim();

export default function ShoppingListPage() {
  const [selectedPlanStartDate, setSelectedPlanStartDate] = useState(
    planOptions[0]?.startDate ?? "",
  );
  const [shoppingList, setShoppingList] = useState<ShoppingListResponse | null>(
    null,
  );
  const [isLoading, setIsLoading] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);

  const activePlan = useMemo(
    () => planOptions.find((plan) => plan.startDate === selectedPlanStartDate),
    [selectedPlanStartDate],
  );

  const activeCategories = useMemo(
    () => shoppingList?.categories ?? [],
    [shoppingList],
  );

  const totalItems = useMemo(
    () =>
      activeCategories.reduce(
        (total, category) => total + category.items.length,
        0,
      ),
    [activeCategories],
  );

  const totalCategories = activeCategories.length;

  const activeSections = useMemo(
    () =>
      activeCategories.map((category) => ({
        id: category.category,
        title: formatCategoryLabel(category.category),
        items: category.items.map((item) => ({
          id: `${item.ingredientId}-${item.unit}`,
          name: item.name,
          amount: formatAmount(item.amount, item.unit),
        })),
      })),
    [activeCategories],
  );

  useEffect(() => {
    if (!selectedPlanStartDate) {
      return;
    }

    let isActive = true;

    const loadShoppingList = async () => {
      setIsLoading(true);
      setLoadError(null);

      try {
        const response = await apiClient.getShoppingList(selectedPlanStartDate);
        if (isActive) {
          setShoppingList(response);
        }
      } catch (error) {
        if (error instanceof ApiError && error.status === 404) {
          if (isActive) {
            setShoppingList({
              startDate: selectedPlanStartDate,
              categories: [],
            });
            setLoadError("No shopping list found for this plan.");
          }
        } else {
          if (error instanceof ApiError) {
            console.error("Failed to load shopping list:", error.body ?? error.message);
          } else if (error instanceof Error) {
            console.error("Failed to load shopping list:", error.message);
          } else {
            console.error("Failed to load shopping list.");
          }

          if (isActive) {
            setLoadError("Unable to load shopping list.");
          }
        }
      } finally {
        if (isActive) {
          setIsLoading(false);
        }
      }
    };

    void loadShoppingList();

    return () => {
      isActive = false;
    };
  }, [selectedPlanStartDate]);

  const summaryLabel = isLoading
    ? "Loading shopping list..."
    : `${totalItems} items across ${totalCategories} categories`;

  return (
    <div className="min-h-screen w-full p-6 sm:p-8">
      <div className="flex flex-wrap items-start gap-6">
        <Sidebar />
        <main className="min-w-[280px] flex-1 space-y-6">
          <header className="flex flex-wrap items-start justify-between gap-4">
            <div className="flex items-center gap-3">
              <span className="grid h-12 w-12 place-items-center rounded-2xl bg-[#eef4ee] text-[#2f6b4f]">
                <CartIcon className="h-5 w-5" />
              </span>
              <div>
                <h1 className="text-2xl font-semibold text-[#1f2a22]">
                  Shopping List
                </h1>
                <p className="text-sm text-[#6c7a70]">
                  {summaryLabel}
                </p>
              </div>
            </div>
            <div className="flex flex-wrap items-center gap-3">
              <div className="flex flex-col gap-1 rounded-2xl border border-[#e1e8dc] bg-white/80 px-4 py-3 shadow-[0_12px_24px_-20px_rgba(32,70,48,0.35)]">
                <span className="text-[11px] font-semibold uppercase tracking-[0.2em] text-[#7b8a7f]">
                  Plan selection
                </span>
                <div className="flex items-center gap-2">
                  <span className="text-sm font-semibold text-[#1f2a22]">
                    {activePlan?.label ?? "Select plan"}
                  </span>
                  <div className="relative">
                    <select
                      aria-label="Select a plan for the shopping list"
                      value={selectedPlanStartDate}
                      onChange={(event) =>
                        setSelectedPlanStartDate(event.target.value)
                      }
                      className="appearance-none bg-transparent pr-6 text-sm font-semibold text-[#2f6b4f] focus:outline-none"
                    >
                      {planOptions.map((plan) => (
                        <option key={plan.startDate} value={plan.startDate}>
                          {plan.label}
                        </option>
                      ))}
                    </select>
                    <ChevronDownIcon className="pointer-events-none absolute right-0 top-1/2 h-4 w-4 -translate-y-1/2 text-[#7b8a7f]" />
                  </div>
                </div>
                <span className="text-xs text-[#7b8a7f]">
                  {activePlan?.subtitle ?? "Pick a plan to view items"}
                </span>
              </div>
            </div>
          </header>

          <section className="space-y-6 rounded-[28px] border border-[#e1e8dc] bg-white/80 p-6 shadow-[0_24px_48px_-36px_rgba(30,60,40,0.45)]">
            <div className="flex flex-wrap items-center justify-between gap-4">
              <div className="space-y-1">
                <h2 className="text-lg font-semibold text-[#1f2a22]">
                  Items for {activePlan?.label ?? "this plan"}
                </h2>
                <p className="text-sm text-[#7b8a7f]">
                  Generated from the dishes in the selected plan
                </p>
              </div>
              <div className="flex w-full max-w-md items-center gap-3 rounded-2xl border border-[#e1e8dc] bg-white/70 px-4 py-3 text-sm text-[#1f2a22] shadow-[0_12px_24px_-20px_rgba(32,70,48,0.35)]">
                <input
                  type="text"
                  placeholder="Add custom item..."
                  className="flex-1 bg-transparent text-sm font-medium text-[#1f2a22] placeholder:text-[#9aa79b] focus:outline-none"
                />
                <button
                  type="button"
                  className="grid h-9 w-9 place-items-center rounded-xl bg-[#2f6b4f] text-white shadow-[0_12px_24px_-18px_rgba(32,78,54,0.9)]"
                  aria-label="Add custom item"
                >
                  <PlusIcon className="h-4 w-4" />
                </button>
              </div>
            </div>

            {loadError ? (
              <div className="rounded-2xl border border-[#f0d6d6] bg-[#fef6f6] px-4 py-3 text-sm font-semibold text-[#a04646]">
                {loadError}
              </div>
            ) : null}
            {isLoading ? (
              <div className="rounded-2xl border border-[#e1e8dc] bg-white/70 px-4 py-3 text-sm font-semibold text-[#6c7a70]">
                Loading shopping list...
              </div>
            ) : null}
            {!isLoading && !loadError && activeSections.length === 0 ? (
              <div className="rounded-2xl border border-[#e1e8dc] bg-white/70 px-4 py-3 text-sm font-semibold text-[#6c7a70]">
                No items found for this plan.
              </div>
            ) : null}
            {!isLoading && !loadError && activeSections.length > 0 ? (
              <div className="space-y-6">
                {activeSections.map((section) => (
                  <div key={section.id} className="space-y-3">
                    <div className="flex items-center gap-3">
                      <span className="rounded-full bg-[#eef4ee] px-3 py-1 text-xs font-semibold text-[#2f6b4f]">
                        {section.title}
                      </span>
                      <span className="h-px flex-1 bg-[#e1e8dc]" />
                      <span className="text-xs font-semibold text-[#7b8a7f]">
                        {section.items.length} items
                      </span>
                    </div>
                    <ul className="space-y-3">
                      {section.items.map((item) => (
                        <li
                          key={item.id}
                          className="rounded-2xl border border-[#e1e8dc] bg-white/70 px-4 py-3 shadow-[0_14px_26px_-22px_rgba(30,60,40,0.35)]"
                        >
                          <div className="flex flex-wrap items-center justify-between gap-3">
                            <div className="space-y-1">
                              <div className="text-sm font-semibold text-[#1f2a22]">
                                {item.name}
                                <span className="ml-2 text-sm font-medium text-[#7b8a7f]">
                                  {item.amount}
                                </span>
                              </div>
                            </div>
                          </div>
                        </li>
                      ))}
                    </ul>
                  </div>
                ))}
              </div>
            ) : null}
          </section>
        </main>
      </div>
    </div>
  );
}

function ChevronDownIcon({ className }: { className?: string }) {
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
      <path d="m6 9 6 6 6-6" />
    </svg>
  );
}

function CartIcon({ className }: { className?: string }) {
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
      <path d="M3 5h2l2.4 9.6a1 1 0 0 0 1 .8h8.7a1 1 0 0 0 1-.7l2.2-6.2H7.4" />
      <circle cx="10" cy="20" r="1.5" />
      <circle cx="18" cy="20" r="1.5" />
    </svg>
  );
}

function PlusIcon({ className }: { className?: string }) {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
    >
      <path d="M12 5v14M5 12h14" />
    </svg>
  );
}
