"use client";

import { useEffect, useMemo, useState } from "react";
import { ApiError, apiClient } from "../../lib/api/client";
import type { DishLookup } from "../../lib/api/models/dishes";
import type {
  WeeklyPlanUpsertRequest,
  WeeklyPlanResponse,
} from "../../lib/api/models/weekly-plans";
import Sidebar from "../components/Sidebar";

type WeekDay = {
  key: string;
  date: Date;
};

const weekStartLookup = new Map<string, number>([
  ["sunday", 0],
  ["monday", 1],
  ["tuesday", 2],
  ["wednesday", 3],
  ["thursday", 4],
  ["friday", 5],
  ["saturday", 6],
]);

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

const offsetFromMonday = (dayIndex: number) => (dayIndex + 6) % 7;

const anchorDate = new Date(2026, 0, 30);
const baseWeekStart = startOfWeek(anchorDate, 1);

const buildPlan = (days: WeekDay[]) =>
  days.reduce<Record<string, string | null>>((accumulator, day) => {
    accumulator[day.key] = null;
    return accumulator;
  }, {});

const monthFormatter = new Intl.DateTimeFormat("en-US", {
  month: "long",
  year: "numeric",
});
const rangeFormatter = new Intl.DateTimeFormat("en-US", {
  month: "short",
  day: "numeric",
});
const yearFormatter = new Intl.DateTimeFormat("en-US", {
  year: "numeric",
});
const weekdayFormatter = new Intl.DateTimeFormat("en-US", {
  weekday: "long",
});
const dayFormatter = new Intl.DateTimeFormat("en-US", {
  month: "short",
  day: "numeric",
});

export default function WeeklyPlannerPage() {
  const [dishOptions, setDishOptions] = useState<DishLookup[]>([]);
  const [dishLoadError, setDishLoadError] = useState<string | null>(null);
  const [isLoadingDishes, setIsLoadingDishes] = useState(true);
  const [startDay, setStartDay] = useState(1);
  const [weekOffset, setWeekOffset] = useState(0);
  const [openDayKey, setOpenDayKey] = useState<string | null>(null);
  const [dishSearchQuery, setDishSearchQuery] = useState("");
  const [saveError, setSaveError] = useState<string | null>(null);
  const [saveMessage, setSaveMessage] = useState<string | null>(null);
  const [isSavingPlan, setIsSavingPlan] = useState(false);
  const [isGeneratingPlan, setIsGeneratingPlan] = useState(false);
  const [isMarkingEaten, setIsMarkingEaten] = useState(false);
  const [isLoadingPlan, setIsLoadingPlan] = useState(false);

  useEffect(() => {
    let isActive = true;

    const loadDishes = async () => {
      setIsLoadingDishes(true);
      setDishLoadError(null);

      try {
        const response = await apiClient.getDishesLookup();
        if (isActive) {
          setDishOptions(response.dishes ?? []);
        }
      } catch (error) {
        if (error instanceof ApiError) {
          console.error("Failed to load dish lookup:", error.body ?? error.message);
        } else if (error instanceof Error) {
          console.error("Failed to load dish lookup:", error.message);
        } else {
          console.error("Failed to load dish lookup.");
        }

        if (isActive) {
          setDishLoadError("Unable to load dishes.");
        }
      } finally {
        if (isActive) {
          setIsLoadingDishes(false);
        }
      }
    };

    void loadDishes();

    return () => {
      isActive = false;
    };
  }, []);

  useEffect(() => {
    let isActive = true;

    const loadSettings = async () => {
      try {
        const response = await apiClient.getPlanningSettings();
        if (isActive) {
          setStartDay(parseWeekStartsOn(response.weekStartsOn));
        }
      } catch (error) {
        if (error instanceof ApiError && error.status === 404) {
          return;
        }

        if (error instanceof ApiError) {
          console.error("Failed to load planning settings:", error.body ?? error.message);
        } else if (error instanceof Error) {
          console.error("Failed to load planning settings:", error.message);
        } else {
          console.error("Failed to load planning settings.");
        }
      }
    };

    void loadSettings();

    return () => {
      isActive = false;
    };
  }, []);

  const dishLookup = useMemo(
    () => new Map(dishOptions.map((dish) => [dish.id, dish])),
    [dishOptions],
  );

  const weekDays = useMemo(() => {
    const weekStart = addDays(baseWeekStart, weekOffset * 7);
    const startDate = addDays(weekStart, offsetFromMonday(startDay));

    return Array.from({ length: 7 }, (_, index) => {
      const date = addDays(startDate, index);

      return {
        key: formatDayKey(date),
        date,
      };
    });
  }, [startDay, weekOffset]);

  const defaultPlan = useMemo(() => buildPlan(weekDays), [weekDays]);
  const [plan, setPlan] = useState<Record<string, string | null>>(
    () => defaultPlan,
  );

  useEffect(() => {
    setPlan(defaultPlan);
    setOpenDayKey(null);
  }, [defaultPlan]);

  const weekRangeLabel = useMemo(() => {
    const startDate = weekDays[0]?.date;
    const endDate = weekDays[6]?.date;

    if (!startDate || !endDate) {
      return monthFormatter.format(weekDays[0]?.date ?? anchorDate);
    }

    const startYear = yearFormatter.format(startDate);
    const endYear = yearFormatter.format(endDate);

    if (startYear === endYear) {
      return `${rangeFormatter.format(startDate)} – ${rangeFormatter.format(endDate)} ${endYear}`;
    }

    return `${rangeFormatter.format(startDate)} ${startYear} – ${rangeFormatter.format(endDate)} ${endYear}`;
  }, [weekDays]);

  const handleToggle = (dayKey: string) => {
    setOpenDayKey((current) => (current === dayKey ? null : dayKey));
    setDishSearchQuery("");
  };

  const handleSelect = (dayKey: string, dishId: string | null) => {
    setPlan((current) => ({
      ...current,
      [dayKey]: dishId,
    }));
    setOpenDayKey(null);
    setDishSearchQuery("");
    setSaveMessage(null);
  };

  const handleCloseMenus = () => {
    setOpenDayKey(null);
    setDishSearchQuery("");
  };

  const handlePreviousWeek = () => {
    handleCloseMenus();
    setWeekOffset((current) => current - 1);
  };

  const handleNextWeek = () => {
    handleCloseMenus();
    setWeekOffset((current) => current + 1);
  };

  const planEntries = useMemo(
    () =>
      weekDays.map((day) => ({
        ...day,
        dishId: plan[day.key] ?? null,
        dish: plan[day.key] ? dishLookup.get(plan[day.key] ?? "") : null,
      })),
    [dishLookup, plan, weekDays],
  );

  const filteredDishOptions = useMemo(() => {
    const query = dishSearchQuery.trim().toLowerCase();
    if (!query) {
      return dishOptions;
    }

    return dishOptions.filter((dish) => {
      const nameMatch = dish.name.toLowerCase().includes(query);
      const cuisineMatch = dish.cuisine.toLowerCase().includes(query);

      return nameMatch || cuisineMatch;
    });
  }, [dishOptions, dishSearchQuery]);

  const formatSelectionType = (value: string) => value.trim().toUpperCase();

  const mapResponseToPlan = (response: WeeklyPlanResponse) => {
    const nextPlan: Record<string, string | null> = {};

    response.days.forEach((day) => {
      const selectionType = formatSelectionType(day.selection.type);
      nextPlan[day.date] = selectionType === "DISH" ? day.selection.dishId ?? null : null;
    });

    return nextPlan;
  };

  const buildUpsertRequest = (): WeeklyPlanUpsertRequest => ({
    days: weekDays.map((day) => {
      const dishId = plan[day.key] ?? null;

      return {
        date: day.key,
        selection: dishId
          ? { type: "DISH", dishId }
          : { type: "EMPTY", dishId: null },
      };
    }),
  });

  const handleSavePlan = async () => {
    setSaveError(null);
    setSaveMessage(null);
    setIsSavingPlan(true);

    try {
      const startDate = weekDays[0]?.key ?? "";
      const payload = buildUpsertRequest();
      const response = await apiClient.upsertWeeklyPlan(startDate, payload);
      setPlan(mapResponseToPlan(response));
      setSaveMessage("Weekly plan saved.");
    } catch (error) {
      if (error instanceof ApiError) {
        console.error("Failed to save weekly plan:", error.body ?? error.message);
      } else if (error instanceof Error) {
        console.error("Failed to save weekly plan:", error.message);
      } else {
        console.error("Failed to save weekly plan.");
      }
      setSaveError("Unable to save weekly plan.");
    } finally {
      setIsSavingPlan(false);
    }
  };

  const handleGeneratePlan = async () => {
    handleCloseMenus();
    setSaveError(null);
    setSaveMessage(null);
    setIsGeneratingPlan(true);

    const startDate = weekDays[0]?.key ?? "";
    if (!startDate) {
      setSaveError("Start date is missing.");
      setIsGeneratingPlan(false);
      return;
    }

    try {
      const response = await apiClient.generateWeeklyPlan(startDate);
      setPlan(mapResponseToPlan(response));
      setSaveMessage("Weekly plan generated.");
    } catch (error) {
      if (error instanceof ApiError) {
        console.error("Failed to generate weekly plan:", error.body ?? error.message);
      } else if (error instanceof Error) {
        console.error("Failed to generate weekly plan:", error.message);
      } else {
        console.error("Failed to generate weekly plan.");
      }
      setSaveError("Unable to generate weekly plan.");
    } finally {
      setIsGeneratingPlan(false);
    }
  };

  const handleMarkEaten = async () => {
    handleCloseMenus();
    setSaveError(null);
    setSaveMessage(null);
    setIsMarkingEaten(true);

    const startDate = weekDays[0]?.key ?? "";
    if (!startDate) {
      setSaveError("Start date is missing.");
      setIsMarkingEaten(false);
      return;
    }

    try {
      await apiClient.markWeeklyPlanEaten(startDate);
      setSaveMessage("Weekly plan marked as eaten.");
    } catch (error) {
      if (error instanceof ApiError) {
        if (error.status === 409) {
          setSaveError("Weekly plan already marked as eaten.");
          return;
        }
        if (error.status === 404) {
          setSaveError("Weekly plan not found.");
          return;
        }
        console.error("Failed to mark weekly plan as eaten:", error.body ?? error.message);
      } else if (error instanceof Error) {
        console.error("Failed to mark weekly plan as eaten:", error.message);
      } else {
        console.error("Failed to mark weekly plan as eaten.");
      }
      setSaveError("Unable to mark weekly plan as eaten.");
    } finally {
      setIsMarkingEaten(false);
    }
  };

  useEffect(() => {
    let isActive = true;

    const loadPlan = async () => {
      const startDate = weekDays[0]?.key ?? "";
      if (!startDate) {
        return;
      }

      setIsLoadingPlan(true);
      setSaveError(null);
      setSaveMessage(null);

      try {
        const response = await apiClient.getWeeklyPlan(startDate);
        if (isActive) {
          setPlan(mapResponseToPlan(response));
        }
      } catch (error) {
        if (error instanceof ApiError && error.status === 404) {
          if (isActive) {
            setPlan(buildPlan(weekDays));
          }
        } else {
          if (error instanceof ApiError) {
            console.error("Failed to load weekly plan:", error.body ?? error.message);
          } else if (error instanceof Error) {
            console.error("Failed to load weekly plan:", error.message);
          } else {
            console.error("Failed to load weekly plan.");
          }
        }
      } finally {
        if (isActive) {
          setIsLoadingPlan(false);
        }
      }
    };

    void loadPlan();

    return () => {
      isActive = false;
    };
  }, [weekDays]);

  return (
    <div className="min-h-screen w-full p-6 sm:p-8">
      <div className="flex flex-wrap items-start gap-6">
        <Sidebar />
        <main className="min-w-[280px] flex-1 space-y-6">
          <header className="flex flex-wrap items-center justify-between gap-4">
            <div className="flex items-center gap-3">
              <span className="grid h-12 w-12 place-items-center rounded-2xl bg-[#eef4ee] text-[#2f6b4f]">
                <CalendarIcon className="h-5 w-5" />
              </span>
              <div>
                <h1 className="text-2xl font-semibold text-[#1f2a22]">
                  Weekly Meal Planner
                </h1>
                <p className="text-sm text-[#6c7a70]">
                  Plan your meals for the week ahead
                </p>
              </div>
            </div>
            <div className="flex flex-wrap items-center gap-3">
              <button
                type="button"
                onClick={handleGeneratePlan}
                disabled={
                  isGeneratingPlan || isLoadingPlan || isSavingPlan || isMarkingEaten
                }
                className="inline-flex items-center gap-2 rounded-full border border-[#d6e0d2] bg-white px-4 py-2 text-sm font-semibold text-[#3b4c42] transition hover:bg-[#f3f6ef]"
              >
                <RefreshIcon className="h-4 w-4" />
                {isGeneratingPlan ? "Generating..." : "Generate Plan"}
              </button>
              <button
                type="button"
                onClick={handleSavePlan}
                disabled={isSavingPlan || isLoadingPlan || isMarkingEaten}
                className="inline-flex items-center gap-2 rounded-full bg-[#2f6b4f] px-4 py-2 text-sm font-semibold text-white shadow-[0_12px_24px_-18px_rgba(32,78,54,0.9)] transition hover:bg-[#2a5c46]"
              >
                <SaveIcon className="h-4 w-4" />
                {isSavingPlan ? "Saving..." : "Save Plan"}
              </button>
              <button
                type="button"
                onClick={handleMarkEaten}
                disabled={isMarkingEaten || isLoadingPlan || isSavingPlan}
                className="inline-flex items-center gap-2 rounded-full border border-[#f1d7b5] bg-[#fff3e6] px-4 py-2 text-sm font-semibold text-[#6a4b2f] transition hover:bg-[#ffe8cf]"
              >
                <CheckCircleIcon className="h-4 w-4" />
                {isMarkingEaten ? "Marking..." : "Mark as eaten"}
              </button>
              {saveMessage ? (
                <span className="text-xs font-semibold text-[#2f6b4f]">
                  {saveMessage}
                </span>
              ) : null}
              {saveError ? (
                <span className="text-xs font-semibold text-[#a04646]">
                  {saveError}
                </span>
              ) : null}
            </div>
          </header>

          <section className="rounded-[28px] border border-[#e1e8dc] bg-white/80 p-6 shadow-[0_20px_48px_-36px_rgba(30,60,40,0.4)]">
            <div className="flex flex-col gap-6">
              <div className="flex flex-wrap items-center justify-between gap-4">
                <button
                  type="button"
                  aria-label="Previous week"
                  onClick={handlePreviousWeek}
                  className="grid h-10 w-10 place-items-center rounded-full border border-[#dfe7d7] bg-white text-[#5c6b60] transition hover:bg-[#f5f7f3]"
                >
                  <ChevronLeftIcon className="h-4 w-4" />
                </button>
                <div className="text-lg font-semibold text-[#1f2a22]">
                  {weekRangeLabel}
                </div>
                <button
                  type="button"
                  aria-label="Next week"
                  onClick={handleNextWeek}
                  className="grid h-10 w-10 place-items-center rounded-full border border-[#dfe7d7] bg-white text-[#5c6b60] transition hover:bg-[#f5f7f3]"
                >
                  <ChevronRightIcon className="h-4 w-4" />
                </button>
              </div>

              <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-7">
                {planEntries.map((day) => {
                  const isOpen = openDayKey === day.key;
                  const dishName = day.dish?.name ?? "Select dish";
                  const cuisine = day.dish?.cuisine ?? "";

                  return (
                    <article
                      key={day.key}
                      className="relative flex min-h-[220px] flex-col gap-4 rounded-3xl border border-[#e1efe4] bg-[#f1faf2] p-4 shadow-[0_18px_36px_-28px_rgba(30,70,45,0.35)]"
                    >
                      <div>
                        <div className="text-sm font-semibold text-[#1f2a22]">
                          {weekdayFormatter.format(day.date)}
                        </div>
                        <div className="text-xs font-semibold text-[#7b8a7f]">
                          {dayFormatter.format(day.date)}
                        </div>
                      </div>

                      <div className="relative">
                        <button
                          type="button"
                          aria-haspopup="listbox"
                          aria-expanded={isOpen}
                          onClick={() => handleToggle(day.key)}
                          className="flex w-full items-center justify-between gap-3 rounded-2xl border border-[#dfe7d7] bg-white/90 px-4 py-2.5 text-left text-sm font-semibold text-[#2a3a2f] shadow-[0_12px_22px_-18px_rgba(28,60,40,0.4)] transition hover:bg-white"
                        >
                          <span
                            className={`truncate ${day.dish ? "" : "text-[#92a097]"}`}
                          >
                            {dishName}
                          </span>
                          <ChevronDownIcon
                            className={`h-4 w-4 text-[#7b8a7f] transition ${
                              isOpen ? "rotate-180" : ""
                            }`}
                          />
                        </button>

                        {isOpen ? (
                          <div
                            role="listbox"
                            className="absolute left-0 right-0 z-20 mt-2 overflow-hidden rounded-2xl border border-[#dfe7d7] bg-white text-sm shadow-[0_20px_40px_-26px_rgba(32,70,45,0.45)]"
                          >
                            <div className="border-b border-[#e4ece1] bg-[#f7fbf6] px-3 py-2">
                              <input
                                type="text"
                                value={dishSearchQuery}
                                onChange={(event) => setDishSearchQuery(event.target.value)}
                                placeholder="Search dishes..."
                                className="w-full rounded-xl border border-[#dfe7d7] bg-white px-3 py-2 text-xs font-semibold text-[#2a3a2f] shadow-[0_8px_16px_-14px_rgba(28,60,40,0.4)] focus:outline-none"
                              />
                            </div>
                            <div className="max-h-56 overflow-y-auto">
                              <button
                                type="button"
                                role="option"
                                aria-selected={!day.dishId}
                                onClick={() => handleSelect(day.key, null)}
                                className={`flex w-full items-center justify-between px-4 py-2.5 text-left transition hover:bg-[#f4f7f1] ${
                                  !day.dishId ? "bg-[#e9f3ea] text-[#2f6b4f]" : ""
                                }`}
                              >
                                <span>No dish</span>
                                {!day.dishId ? <CheckIcon className="h-4 w-4" /> : null}
                              </button>
                              {isLoadingDishes ? (
                                <div className="px-4 py-2.5 text-xs font-semibold text-[#7b8a7f]">
                                  Loading dishes...
                                </div>
                              ) : dishLoadError ? (
                                <div className="px-4 py-2.5 text-xs font-semibold text-[#9f4c4c]">
                                  {dishLoadError}
                                </div>
                              ) : filteredDishOptions.length === 0 ? (
                                <div className="px-4 py-2.5 text-xs font-semibold text-[#7b8a7f]">
                                  No dishes found
                                </div>
                              ) : (
                                filteredDishOptions.map((dish) => {
                                  const isSelected = dish.id === day.dishId;

                                  return (
                                    <button
                                      key={dish.id}
                                      type="button"
                                      role="option"
                                      aria-selected={isSelected}
                                      onClick={() => handleSelect(day.key, dish.id)}
                                      className={`flex w-full items-center justify-between px-4 py-2.5 text-left transition hover:bg-[#f4f7f1] ${
                                        isSelected
                                          ? "bg-[#e9f3ea] text-[#2f6b4f]"
                                          : "text-[#2a3a2f]"
                                      }`}
                                    >
                                      <span>{dish.name}</span>
                                      {isSelected ? (
                                        <CheckIcon className="h-4 w-4" />
                                      ) : null}
                                    </button>
                                  );
                                })
                              )}
                            </div>
                          </div>
                        ) : null}
                      </div>

                      <div className="mt-auto">
                        {cuisine ? (
                          <span className="inline-flex rounded-full bg-[#e7f0e8] px-3 py-1 text-xs font-semibold text-[#3a5a44]">
                            {cuisine}
                          </span>
                        ) : (
                          <span className="text-xs font-semibold text-[#a0ada5]">
                            No cuisine selected
                          </span>
                        )}
                      </div>
                    </article>
                  );
                })}
              </div>
            </div>
          </section>
        </main>
      </div>
    </div>
  );
}

type IconProps = {
  className?: string;
};

function CalendarIcon({ className }: IconProps) {
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
      <path d="M7 3v3M17 3v3" />
      <rect x="4" y="6" width="16" height="15" rx="2" />
      <path d="M4 10h16" />
    </svg>
  );
}

function RefreshIcon({ className }: IconProps) {
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
      <path d="M20 12a8 8 0 1 1-2.3-5.7" />
      <path d="M20 4v6h-6" />
    </svg>
  );
}

function SaveIcon({ className }: IconProps) {
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
      <path d="M5 4h11l3 3v13H5z" />
      <path d="M8 4v6h8" />
      <path d="M8 18h8" />
    </svg>
  );
}

function ChevronDownIcon({ className }: IconProps) {
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

function ChevronLeftIcon({ className }: IconProps) {
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
      <path d="m15 18-6-6 6-6" />
    </svg>
  );
}

function ChevronRightIcon({ className }: IconProps) {
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
      <path d="m9 6 6 6-6 6" />
    </svg>
  );
}

function CheckIcon({ className }: IconProps) {
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
      <path d="M6 12l4 4 8-8" />
    </svg>
  );
}

function CheckCircleIcon({ className }: IconProps) {
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
      <circle cx="12" cy="12" r="9" />
      <path d="M8.5 12.5 11 15l4.5-5" />
    </svg>
  );
}
