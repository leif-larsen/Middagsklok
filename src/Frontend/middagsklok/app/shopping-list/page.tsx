"use client";

import { useEffect, useMemo, useState } from "react";
import { ApiError, apiClient } from "../../lib/api/client";
import type { ShoppingListResponse } from "../../lib/api/models/shopping-list";
import type { WeeklyPlanSummary } from "../../lib/api/models/weekly-plans";
import Sidebar from "../components/Sidebar";

const weekStartLookup = new Map<string, number>([
  ["sunday", 0],
  ["monday", 1],
  ["tuesday", 2],
  ["wednesday", 3],
  ["thursday", 4],
  ["friday", 5],
  ["saturday", 6],
]);

const rangeFormatter = new Intl.DateTimeFormat("nb-NO", {
  month: "short",
  day: "numeric",
});

const categoryLabels: Record<string, string> = {
  Produce: "Frukt og grønt",
  Meat: "Kjøtt",
  Poultry: "Fjærkre",
  Seafood: "Sjømat",
  DairyAndEggs: "Meieri og egg",
  PastaAndGrains: "Pasta og korn",
  Bakery: "Bakervarer",
  CannedGoods: "Hermetikk",
  FrozenFoods: "Frossenmat",
  Condiments: "Sauser og dressinger",
  SpicesAndHerbs: "Krydder og urter",
  Baking: "Baking",
  OilsAndVinegars: "Oljer og eddik",
  Beverages: "Drikke",
  Snacks: "Snacks",
  Other: "Annet",
};

const formatCategoryLabel = (value: string) => {
  if (categoryLabels[value]) {
    return categoryLabels[value];
  }

  return value.replace(/([a-z])([A-Z])/g, "$1 $2");
};

const parseDate = (value: string) => new Date(`${value}T00:00:00`);

const parseWeekStartsOn = (value: string | null | undefined) => {
  const normalized = value?.trim().toLowerCase() ?? "";
  return weekStartLookup.get(normalized) ?? 1;
};

const formatDayKey = (date: Date) => {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");

  return `${year}-${month}-${day}`;
};

const addDays = (date: Date, amount: number) => {
  const next = new Date(date);
  next.setDate(date.getDate() + amount);

  return next;
};

const startOfWeek = (date: Date, weekStartsOn: number) => {
  const dayIndex = date.getDay();
  const diff = (dayIndex - weekStartsOn + 7) % 7;

  return addDays(date, -diff);
};

const pickClosestPlanStartDate = (
  plans: WeeklyPlanSummary[],
  targetDate: Date,
) => {
  if (plans.length === 0) {
    return "";
  }

  const targetTime = targetDate.getTime();
  let bestPlan = plans[0]!;
  let bestDistance = Math.abs(parseDate(bestPlan.startDate).getTime() - targetTime);

  for (let index = 1; index < plans.length; index += 1) {
    const candidate = plans[index]!;
    const candidateTime = parseDate(candidate.startDate).getTime();
    const candidateDistance = Math.abs(candidateTime - targetTime);
    if (candidateDistance < bestDistance) {
      bestPlan = candidate;
      bestDistance = candidateDistance;
      continue;
    }

    if (candidateDistance !== bestDistance) {
      continue;
    }

    const bestTime = parseDate(bestPlan.startDate).getTime();
    const candidateIsFuture = candidateTime >= targetTime;
    const bestIsFuture = bestTime >= targetTime;
    if (candidateIsFuture && !bestIsFuture) {
      bestPlan = candidate;
      bestDistance = candidateDistance;
      continue;
    }

    if (candidateIsFuture === bestIsFuture && candidateTime < bestTime) {
      bestPlan = candidate;
      bestDistance = candidateDistance;
    }
  }

  return bestPlan.startDate;
};

const formatRangeLabel = (startDate: string, endDate: string) => {
  const start = parseDate(startDate);
  const end = parseDate(endDate);
  const startIsValid = !Number.isNaN(start.getTime());
  const endIsValid = !Number.isNaN(end.getTime());

  if (!startIsValid || !endIsValid) {
    return `${startDate} - ${endDate}`;
  }

  return `${rangeFormatter.format(start)} - ${rangeFormatter.format(end)}`;
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
  const [today] = useState(() => new Date());
  const [planOptions, setPlanOptions] = useState<WeeklyPlanSummary[]>([]);
  const [selectedPlanStartDate, setSelectedPlanStartDate] = useState("");
  const [shoppingList, setShoppingList] = useState<ShoppingListResponse | null>(
    null,
  );
  const [isLoading, setIsLoading] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [isLoadingPlans, setIsLoadingPlans] = useState(true);
  const [planLoadError, setPlanLoadError] = useState<string | null>(null);

  const activePlan = useMemo(
    () => planOptions.find((plan) => plan.startDate === selectedPlanStartDate),
    [planOptions, selectedPlanStartDate],
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
    let isActive = true;

    const loadPlans = async () => {
      setIsLoadingPlans(true);
      setPlanLoadError(null);

      try {
        let weekStartsOn = 1;
        try {
          const settings = await apiClient.getPlanningSettings();
          weekStartsOn = parseWeekStartsOn(settings.weekStartsOn);
        } catch (error) {
          if (!(error instanceof ApiError && error.status === 404)) {
            if (error instanceof ApiError) {
              console.error("Failed to load planning settings:", error.body ?? error.message);
            } else if (error instanceof Error) {
              console.error("Failed to load planning settings:", error.message);
            } else {
              console.error("Failed to load planning settings.");
            }
          }
        }

        const response = await apiClient.getWeeklyPlans();
        if (!isActive) {
          return;
        }

        const plans = response.plans ?? [];
        const targetStartDate = formatDayKey(addDays(startOfWeek(today, weekStartsOn), 7));

        setPlanOptions(plans);
        setSelectedPlanStartDate((current) => {
          if (plans.length === 0) {
            setShoppingList(null);
            return "";
          }

          if (plans.some((plan) => plan.startDate === current)) {
            return current;
          }

          const exactMatch = plans.find((plan) => plan.startDate === targetStartDate);
          if (exactMatch) {
            return exactMatch.startDate;
          }

          return pickClosestPlanStartDate(plans, parseDate(targetStartDate));
        });
      } catch (error) {
        if (error instanceof ApiError) {
          console.error("Failed to load weekly plans:", error.body ?? error.message);
        } else if (error instanceof Error) {
          console.error("Failed to load weekly plans:", error.message);
        } else {
          console.error("Failed to load weekly plans.");
        }

        if (isActive) {
          setPlanLoadError("Kunne ikke laste planer.");
          setPlanOptions([]);
          setSelectedPlanStartDate("");
          setShoppingList(null);
        }
      } finally {
        if (isActive) {
          setIsLoadingPlans(false);
        }
      }
    };

    void loadPlans();

    return () => {
      isActive = false;
    };
  }, [today]);

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
            setLoadError("Ingen handleliste funnet for denne planen.");
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
            setLoadError("Kunne ikke laste handleliste.");
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
    ? "Laster handleliste..."
    : `${totalItems} varer fordelt på ${totalCategories} kategorier`;
  const planHelperLabel = isLoadingPlans
    ? "Laster planer..."
    : planLoadError
      ? planLoadError
      : planOptions.length === 0
        ? "Ingen planer tilgjengelig"
        : "Velg en plan for å se varer";
  const activePlanLabel = activePlan
    ? formatRangeLabel(activePlan.startDate, activePlan.endDate)
    : "Velg plan";
  const itemsHeading = activePlan
    ? `Varer for ${activePlanLabel}`
    : "Varer for denne planen";

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
                  Handleliste
                </h1>
                <p className="text-sm text-[#6c7a70]">
                  {summaryLabel}
                </p>
              </div>
            </div>
            <div className="flex flex-wrap items-center gap-3">
              <div className="flex flex-col gap-1 rounded-2xl border border-[#e1e8dc] bg-white/80 px-4 py-3 shadow-[0_12px_24px_-20px_rgba(32,70,48,0.35)]">
                <span className="text-[11px] font-semibold uppercase tracking-[0.2em] text-[#7b8a7f]">
                  Planvalg
                </span>
                <div className="flex items-center gap-2">
                  <span className="text-sm font-semibold text-[#1f2a22]">
                    {activePlanLabel}
                  </span>
                  <div className="relative">
                    <select
                      aria-label="Select a plan for the shopping list"
                      value={selectedPlanStartDate}
                      onChange={(event) =>
                        setSelectedPlanStartDate(event.target.value)
                      }
                      disabled={isLoadingPlans || planOptions.length === 0 || !!planLoadError}
                      className="appearance-none bg-transparent pr-6 text-sm font-semibold text-[#2f6b4f] focus:outline-none"
                    >
                      {planOptions.map((plan) => (
                        <option key={plan.startDate} value={plan.startDate}>
                          {formatRangeLabel(plan.startDate, plan.endDate)}
                        </option>
                      ))}
                    </select>
                    <ChevronDownIcon className="pointer-events-none absolute right-0 top-1/2 h-4 w-4 -translate-y-1/2 text-[#7b8a7f]" />
                  </div>
                </div>
                <span className="text-xs text-[#7b8a7f]">
                  {planHelperLabel}
                </span>
              </div>
            </div>
          </header>

          <section className="space-y-6 rounded-[28px] border border-[#e1e8dc] bg-white/80 p-6 shadow-[0_24px_48px_-36px_rgba(30,60,40,0.45)]">
            <div className="flex flex-wrap items-center justify-between gap-4">
              <div className="space-y-1">
                <h2 className="text-lg font-semibold text-[#1f2a22]">
                  {itemsHeading}
                </h2>
                <p className="text-sm text-[#7b8a7f]">
                  Generert fra rettene i den valgte planen
                </p>
              </div>
            </div>

            {loadError ? (
              <div className="rounded-2xl border border-[#f0d6d6] bg-[#fef6f6] px-4 py-3 text-sm font-semibold text-[#a04646]">
                {loadError}
              </div>
            ) : null}
            {isLoading ? (
              <div className="rounded-2xl border border-[#e1e8dc] bg-white/70 px-4 py-3 text-sm font-semibold text-[#6c7a70]">
                Laster handleliste...
              </div>
            ) : null}
            {!isLoading && !loadError && activeSections.length === 0 ? (
              <div className="rounded-2xl border border-[#e1e8dc] bg-white/70 px-4 py-3 text-sm font-semibold text-[#6c7a70]">
                Ingen varer funnet for denne planen.
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
                        {section.items.length} varer
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
