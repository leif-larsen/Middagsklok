"use client";

import { useEffect, useState } from "react";
import { ApiError, apiClient } from "../../lib/api/client";
import Sidebar from "../components/Sidebar";

const dishTypeOptions = [
  "Pasta",
  "Risbolle",
  "Nudler",
  "Suppe og gryte",
  "Salat",
  "Wrap og taco",
  "Pizza og pai",
  "Gryte og form",
  "Sandwich og burger",
  "Protein og grønt",
  "Frokost til middag",
  "Snackbrett",
  "Annet",
];

const restrictionOptions = [
  "Skalldyr",
  "Peanøtter",
  "Trenøtter",
  "Meieri",
  "Egg",
  "Soya",
  "Hvete",
  "Fisk",
];

const weekStartOptions = [
  { value: "Monday", label: "Mandag", dayIndex: 1 },
  { value: "Tuesday", label: "Tirsdag", dayIndex: 2 },
  { value: "Wednesday", label: "Onsdag", dayIndex: 3 },
  { value: "Thursday", label: "Torsdag", dayIndex: 4 },
  { value: "Friday", label: "Fredag", dayIndex: 5 },
  { value: "Saturday", label: "Lørdag", dayIndex: 6 },
  { value: "Sunday", label: "Søndag", dayIndex: 0 },
];

type ToggleRowProps = {
  label: string;
  description: string;
  checked: boolean;
  onToggle: () => void;
};

function ToggleRow({
  label,
  description,
  checked,
  onToggle,
}: ToggleRowProps) {
  return (
    <div className="flex items-center justify-between gap-4">
      <div>
        <div className="text-sm font-semibold text-[#1f2a22]">{label}</div>
        <p className="text-xs text-[#7b8a7f]">{description}</p>
      </div>
      <button
        type="button"
        role="switch"
        aria-checked={checked}
        onClick={onToggle}
        className={`relative inline-flex h-6 w-11 items-center rounded-full border border-[#d8e4d7] transition ${
          checked ? "bg-[#2f6b4f]" : "bg-[#e4ebe2]"
        }`}
      >
        <span
          className={`inline-block h-5 w-5 rounded-full bg-white shadow-[0_6px_12px_-8px_rgba(18,40,26,0.45)] transition ${
            checked ? "translate-x-5" : "translate-x-1"
          }`}
        />
      </button>
    </div>
  );
}

export default function SettingsPage() {
  const [allowRepeats, setAllowRepeats] = useState(false);
  const [includeWeekends, setIncludeWeekends] = useState(true);
  const [autoGeneratePlans, setAutoGeneratePlans] = useState(true);
  const [diversityScore, setDiversityScore] = useState(70);
  const [maxPrepMinutes, setMaxPrepMinutes] = useState(60);
  const [defaultServings, setDefaultServings] = useState(4);
  const [weekStartsOn, setWeekStartsOn] = useState("Monday");
  const [seafoodPerWeek, setSeafoodPerWeek] = useState(2);
  const [daysBetween, setDaysBetween] = useState(14);
  const [isLoadingSettings, setIsLoadingSettings] = useState(true);
  const [settingsLoadError, setSettingsLoadError] = useState<string | null>(null);
  const [isSavingSettings, setIsSavingSettings] = useState(false);
  const [saveMessage, setSaveMessage] = useState<string | null>(null);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [preferredCategories, setPreferredCategories] = useState<string[]>([
    "Pasta",
    "Risbolle",
    "Wrap og taco",
  ]);
  const [excludedIngredients, setExcludedIngredients] = useState<string[]>([
    "Skalldyr",
    "Peanøtter",
  ]);

  const togglePreferredCategory = (category: string) => {
    setPreferredCategories((current) =>
      current.includes(category)
        ? current.filter((item) => item !== category)
        : [...current, category],
    );
  };

  const toggleRestrictedIngredient = (ingredient: string) => {
    setExcludedIngredients((current) =>
      current.includes(ingredient)
        ? current.filter((item) => item !== ingredient)
        : [...current, ingredient],
    );
  };

  useEffect(() => {
    let isActive = true;

    const loadSettings = async () => {
      setIsLoadingSettings(true);
      setSettingsLoadError(null);

      try {
        const response = await apiClient.getPlanningSettings();
        if (isActive) {
          const matched = weekStartOptions.find(
            (option) =>
              option.value.toLowerCase()
              === response.weekStartsOn.trim().toLowerCase(),
          );
          setWeekStartsOn(matched?.value ?? "Monday");
          setSeafoodPerWeek(response.seafoodPerWeek ?? 2);
          setDaysBetween(response.daysBetween ?? 14);
        }
      } catch (error) {
        if (error instanceof ApiError && error.status === 404) {
          if (isActive) {
            setWeekStartsOn("Monday");
            setSeafoodPerWeek(2);
            setDaysBetween(14);
          }
        } else {
          if (error instanceof ApiError) {
            console.error("Failed to load planning settings:", error.body ?? error.message);
          } else if (error instanceof Error) {
            console.error("Failed to load planning settings:", error.message);
          } else {
            console.error("Failed to load planning settings.");
          }

          if (isActive) {
            setSettingsLoadError("Kunne ikke laste planleggingsinnstillinger.");
          }
        }
      } finally {
        if (isActive) {
          setIsLoadingSettings(false);
        }
      }
    };

    void loadSettings();

    return () => {
      isActive = false;
    };
  }, []);

  const handleSaveSettings = async () => {
    setIsSavingSettings(true);
    setSaveError(null);
    setSaveMessage(null);

    try {
      const response = await apiClient.upsertPlanningSettings({
        weekStartsOn,
        seafoodPerWeek,
        daysBetween,
      });
      setWeekStartsOn(response.weekStartsOn ?? weekStartsOn);
      setSeafoodPerWeek(response.seafoodPerWeek ?? seafoodPerWeek);
      setDaysBetween(response.daysBetween ?? daysBetween);
      setSaveMessage("Innstillinger lagret.");
    } catch (error) {
      if (error instanceof ApiError) {
        console.error("Failed to save planning settings:", error.body ?? error.message);
      } else if (error instanceof Error) {
        console.error("Failed to save planning settings:", error.message);
      } else {
        console.error("Failed to save planning settings.");
      }

      setSaveError("Kunne ikke lagre innstillinger.");
    } finally {
      setIsSavingSettings(false);
    }
  };

  return (
    <div className="min-h-screen w-full p-6 sm:p-8">
      <div className="flex flex-wrap items-start gap-6">
        <Sidebar />
        <main className="min-w-[280px] flex-1 space-y-6">
          <header className="flex flex-wrap items-center justify-between gap-4">
            <div className="flex items-center gap-3">
              <span className="grid h-12 w-12 place-items-center rounded-2xl bg-[#eef4ee] text-[#2f6b4f]">
                <SettingsIcon className="h-5 w-5" />
              </span>
              <div>
                <h1 className="text-2xl font-semibold text-[#1f2a22]">
                  Innstillinger
                </h1>
                <p className="text-sm text-[#6c7a70]">
                  Konfigurer planleggingsregler og preferanser
                </p>
              </div>
            </div>
            <button
              type="button"
              onClick={handleSaveSettings}
              disabled={isSavingSettings}
              className="inline-flex items-center gap-2 rounded-full bg-[#2f6b4f] px-4 py-2 text-sm font-semibold text-white shadow-[0_12px_24px_-18px_rgba(32,78,54,0.9)] transition hover:bg-[#2a5c46]"
            >
              <SaveIcon className="h-4 w-4" />
              {isSavingSettings ? "Lagrer..." : "Lagre innstillinger"}
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
          </header>

          <div className="grid gap-6 xl:grid-cols-2">
            <div className="space-y-6">
              <section className="rounded-[28px] border border-[#e1e8dc] bg-white/80 p-6 shadow-[0_20px_48px_-36px_rgba(30,60,40,0.4)]">
                <div className="space-y-1">
                  <h2 className="text-lg font-semibold text-[#1f2a22]">
                    Ukentlige planleggingsregler
                  </h2>
                  <p className="text-sm text-[#7b8a7f]">
                    Angi regler for automatisk ukesmenyoppsett
                  </p>
                </div>

                <div className="mt-5 space-y-5">
                  <label className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-[#e1e8dc] bg-white/70 px-4 py-3 text-sm font-semibold text-[#1f2a22] shadow-[0_12px_24px_-20px_rgba(32,70,48,0.35)]">
                    <span>Uke starter på</span>
                    <span className="relative">
                      <select
                        aria-label="Uke starter på"
                        value={weekStartsOn}
                        onChange={(event) => setWeekStartsOn(event.target.value)}
                        disabled={isLoadingSettings}
                        className="appearance-none bg-transparent pr-6 text-sm font-semibold text-[#2f6b4f] focus:outline-none"
                      >
                        {weekStartOptions.map((option) => (
                          <option key={option.value} value={option.value}>
                            {option.label}
                          </option>
                        ))}
                      </select>
                      <ChevronDownIcon className="pointer-events-none absolute right-0 top-1/2 h-4 w-4 -translate-y-1/2 text-[#7b8a7f]" />
                    </span>
                  </label>
                  {settingsLoadError ? (
                    <p className="text-xs font-semibold text-[#a04646]">
                      {settingsLoadError}
                    </p>
                  ) : null}
                  <div className="space-y-2">
                    <div className="flex items-center justify-between text-sm font-semibold text-[#1f2a22]">
                      <span>Sjømat per uke</span>
                      <span className="text-[#2f6b4f]">{seafoodPerWeek}</span>
                    </div>
                    <input
                      type="range"
                      min={0}
                      max={7}
                      value={seafoodPerWeek}
                      onChange={(event) =>
                        setSeafoodPerWeek(Number(event.target.value))
                      }
                      className="h-2 w-full cursor-pointer appearance-none rounded-full bg-[#e2e8dc] accent-[#2f6b4f]"
                    />
                  </div>
                  <div className="space-y-2">
                    <div className="flex items-center justify-between text-sm font-semibold text-[#1f2a22]">
                      <span>Dager mellom</span>
                      <span className="text-[#2f6b4f]">{daysBetween}</span>
                    </div>
                    <input
                      type="range"
                      min={0}
                      max={30}
                      value={daysBetween}
                      onChange={(event) =>
                        setDaysBetween(Number(event.target.value))
                      }
                      className="h-2 w-full cursor-pointer appearance-none rounded-full bg-[#e2e8dc] accent-[#2f6b4f]"
                    />
                    <p className="text-xs text-[#7b8a7f]">
                      Lavere score gis til retter spist innen dette antall dager, men de kan fortsatt velges.
                    </p>
                  </div>

                  <div className="space-y-4 border-t border-[#e1e8dc] pt-4">
                    <ToggleRow
                      label="Tillat gjentagende retter"
                      description="Samme rett kan forekomme flere ganger i løpet av en uke"
                      checked={allowRepeats}
                      onToggle={() => setAllowRepeats((current) => !current)}
                    />
                    <ToggleRow
                      label="Inkluder helger"
                      description="Generer planer for lørdag og søndag"
                      checked={includeWeekends}
                      onToggle={() => setIncludeWeekends((current) => !current)}
                    />
                    <ToggleRow
                      label="Autogenerer planer"
                      description="Lag nye ukesplaner automatisk"
                      checked={autoGeneratePlans}
                      onToggle={() => setAutoGeneratePlans((current) => !current)}
                    />
                  </div>
                </div>
              </section>

              <section className="rounded-[28px] border border-[#e1e8dc] bg-white/80 p-6 shadow-[0_20px_48px_-36px_rgba(30,60,40,0.4)]">
                <div className="space-y-1">
                  <h2 className="text-lg font-semibold text-[#1f2a22]">
                    Matlagingsbegrensninger
                  </h2>
                  <p className="text-sm text-[#7b8a7f]">
                    Angi tids- og porsjonspreferanser
                  </p>
                </div>

                <div className="mt-5 space-y-4">
                  <label className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-[#e1e8dc] bg-white/70 px-4 py-3 text-sm font-semibold text-[#1f2a22] shadow-[0_12px_24px_-20px_rgba(32,70,48,0.35)]">
                    <span>Maks forberedelsestid (minutter)</span>
                    <input
                      type="number"
                      min={10}
                      max={180}
                      value={maxPrepMinutes}
                      onChange={(event) =>
                        setMaxPrepMinutes(Number(event.target.value))
                      }
                      className="w-20 rounded-xl border border-[#dfe7d7] bg-white px-3 py-2 text-right text-sm font-semibold text-[#2f6b4f] focus:outline-none"
                    />
                  </label>

                  <label className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-[#e1e8dc] bg-white/70 px-4 py-3 text-sm font-semibold text-[#1f2a22] shadow-[0_12px_24px_-20px_rgba(32,70,48,0.35)]">
                    <span>Standard porsjoner</span>
                    <input
                      type="number"
                      min={1}
                      max={12}
                      value={defaultServings}
                      onChange={(event) =>
                        setDefaultServings(Number(event.target.value))
                      }
                      className="w-20 rounded-xl border border-[#dfe7d7] bg-white px-3 py-2 text-right text-sm font-semibold text-[#2f6b4f] focus:outline-none"
                    />
                  </label>
                </div>
              </section>
            </div>

            <div className="space-y-6">
              <section className="rounded-[28px] border border-[#e1e8dc] bg-white/80 p-6 shadow-[0_20px_48px_-36px_rgba(30,60,40,0.4)]">
                <div className="space-y-1">
                  <h2 className="text-lg font-semibold text-[#1f2a22]">
                    Variasjon og preferanser
                  </h2>
                  <p className="text-sm text-[#7b8a7f]">
                    Styr variasjon og kategoriefordeling
                  </p>
                </div>

                <div className="mt-5 space-y-5">
                  <div className="space-y-2">
                    <div className="flex items-center justify-between text-sm font-semibold text-[#1f2a22]">
                      <span>Variasjonspoeng</span>
                      <span className="text-[#2f6b4f]">{diversityScore}%</span>
                    </div>
                    <p className="text-xs text-[#7b8a7f]">
                      Høyere verdier prioriterer variasjon i ukesplaner
                    </p>
                    <input
                      type="range"
                      min={0}
                      max={100}
                      value={diversityScore}
                      onChange={(event) =>
                        setDiversityScore(Number(event.target.value))
                      }
                      className="h-2 w-full cursor-pointer appearance-none rounded-full bg-[#e2e8dc] accent-[#2f6b4f]"
                    />
                  </div>

                  <div>
                    <div className="text-sm font-semibold text-[#1f2a22]">
                      Foretrukne kategorier
                    </div>
                    <div className="mt-3 flex flex-wrap gap-2">
                      {dishTypeOptions.map((category) => {
                        const isSelected =
                          preferredCategories.includes(category);

                        return (
                          <button
                            key={category}
                            type="button"
                            onClick={() => togglePreferredCategory(category)}
                            className={`rounded-full border px-3 py-1 text-xs font-semibold transition ${
                              isSelected
                                ? "border-[#2f6b4f] bg-[#2f6b4f] text-white"
                                : "border-[#dfe7d7] bg-white text-[#4b5b51] hover:bg-[#f3f7f1]"
                            }`}
                          >
                            {category}
                          </button>
                        );
                      })}
                    </div>
                  </div>
                </div>
              </section>

              <section className="rounded-[28px] border border-[#e1e8dc] bg-white/80 p-6 shadow-[0_20px_48px_-36px_rgba(30,60,40,0.4)]">
                <div className="space-y-1">
                  <h2 className="text-lg font-semibold text-[#1f2a22]">
                    Kostholdsrestriksjoner
                  </h2>
                  <p className="text-sm text-[#7b8a7f]">
                    Ekskluder ingredienser fra menyplanlegging
                  </p>
                </div>

                <div className="mt-4">
                  <div className="text-sm font-semibold text-[#1f2a22]">
                    Ekskluderte ingredienser
                  </div>
                  <div className="mt-3 flex flex-wrap gap-2">
                    {restrictionOptions.map((ingredient) => {
                      const isExcluded =
                        excludedIngredients.includes(ingredient);

                      return (
                        <button
                          key={ingredient}
                          type="button"
                          onClick={() => toggleRestrictedIngredient(ingredient)}
                          className={`rounded-full border px-3 py-1 text-xs font-semibold transition ${
                            isExcluded
                              ? "border-[#d76b6b] bg-[#d76b6b] text-white"
                              : "border-[#dfe7d7] bg-white text-[#4b5b51] hover:bg-[#f3f7f1]"
                          }`}
                        >
                          {ingredient}
                        </button>
                      );
                    })}
                  </div>
                  <p className="mt-3 text-xs text-[#7b8a7f]">
                    Klikk for å slå av/på ingredienser. Røde merker er ekskludert
                    fra menyplanlegging.
                  </p>
                </div>
              </section>
            </div>
          </div>

          <section className="flex flex-wrap items-center gap-4 rounded-2xl border border-[#dfe7d7] bg-[#eaf7ea] px-5 py-4 text-sm text-[#2f5a3d] shadow-[0_18px_36px_-28px_rgba(35,70,45,0.35)]">
            <span className="grid h-10 w-10 place-items-center rounded-2xl bg-white text-[#2f6b4f]">
              <TipIcon className="h-5 w-5" />
            </span>
            <div>
              <div className="text-sm font-semibold text-[#1f2a22]">
                Tips
              </div>
              <p className="text-xs text-[#6c7a70]">
                Juster variasjonspoengene for å styre hvor mye variasjon du vil
                ha i ukesplanene dine. Høyere poeng sikrer ulike retttyper
                og ingredienser gjennom uken.
              </p>
            </div>
          </section>
        </main>
      </div>
    </div>
  );
}

function SettingsIcon({ className }: { className?: string }) {
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
      <path d="M12 3.5v2.2M12 18.3v2.2M4.7 7.3l1.6 1.2M17.7 16.6l1.6 1.2M3.5 12h2.2M18.3 12h2.2M4.7 16.6l1.6-1.2M17.7 7.3l1.6-1.2" />
      <circle cx="12" cy="12" r="3.4" />
    </svg>
  );
}

function SaveIcon({ className }: { className?: string }) {
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
      <path d="M4 5.5a2 2 0 0 1 2-2h9.5l4.5 4.5v8.5a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2Z" />
      <path d="M8 3.5v5h7v-5" />
      <path d="M8 20v-6h8v6" />
    </svg>
  );
}

function TipIcon({ className }: { className?: string }) {
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
      <path d="M9 18h6" />
      <path d="M10 21h4" />
      <path d="M8 13a4 4 0 1 1 8 0c0 1.6-.8 2.4-1.6 3.2-.6.6-1 1.2-1.1 2.3h-2.8c-.1-1.1-.5-1.7-1.1-2.3C8.8 15.4 8 14.6 8 13Z" />
    </svg>
  );
}

function ChevronDownIcon({ className }: { className?: string }) {
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
      <path d="m6 9 6 6 6-6" />
    </svg>
  );
}
