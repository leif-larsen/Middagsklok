"use client";

import Sidebar from "../components/Sidebar";
import Modal from "../components/Modal";
import { apiClient, ApiError } from "../../lib/api/client";
import { useEffect, useMemo, useState } from "react";

type Ingredient = {
  id: string;
  label: string;
};

type Dish = {
  id: string;
  name: string;
  cuisine: string;
  prepMinutes: number;
  cookMinutes: number;
  serves: number;
  instructions?: string | null;
  ingredients: Ingredient[];
};

const emptyDish: Dish = {
  id: "new-dish",
  name: "",
  cuisine: "",
  prepMinutes: 0,
  cookMinutes: 0,
  serves: 0,
  instructions: "",
  ingredients: [],
};

const formatMinutes = (value: number) => `${value}m`;

export default function DishesPage() {
  const [dishes, setDishes] = useState<Dish[]>([]);
  const [selectedDish, setSelectedDish] = useState<Dish | null>(null);
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [isImportOpen, setIsImportOpen] = useState(false);
  const [importFile, setImportFile] = useState<File | null>(null);
  const [isImporting, setIsImporting] = useState(false);
  const [importError, setImportError] = useState<string | null>(null);
  const [importResult, setImportResult] = useState<{
    attempted: number;
    imported: number;
    skipped: number;
    failed: number;
    failures: { dishName?: string | null; reason: string; ingredientName?: string | null }[];
  } | null>(null);

  useEffect(() => {
    let isActive = true;

    const loadDishes = async () => {
      try {
        const response = await apiClient.getDishes();
        if (isActive) {
          setDishes(response.dishes ?? []);
        }
      } catch (error) {
        if (error instanceof ApiError) {
          console.error("Failed to load dishes:", error.body ?? error.message);
        } else if (error instanceof Error) {
          console.error("Failed to load dishes:", error.message);
        } else {
          console.error("Failed to load dishes.");
        }
      }
    };

    void loadDishes();

    return () => {
      isActive = false;
    };
  }, []);

  const activeDish = useMemo(
    () => (isCreateOpen ? emptyDish : selectedDish ?? emptyDish),
    [isCreateOpen, selectedDish],
  );

  const isModalOpen = isCreateOpen || selectedDish !== null;
  const isEditMode = selectedDish !== null;

  const closeModal = () => {
    setIsCreateOpen(false);
    setSelectedDish(null);
  };

  const closeImportModal = () => {
    setIsImportOpen(false);
    setImportFile(null);
    setImportError(null);
    setImportResult(null);
  };

  const parseImportPayload = async (file: File) => {
    const raw = await file.text();
    const parsed = JSON.parse(raw) as unknown;

    if (Array.isArray(parsed)) {
      return { dishes: parsed };
    }

    if (parsed && typeof parsed === "object" && "dishes" in parsed) {
      return parsed as { dishes?: unknown };
    }

    throw new Error("Invalid JSON format. Expected an array or an object with a dishes property.");
  };

  const handleImport = async () => {
    if (!importFile || isImporting) {
      return;
    }

    setIsImporting(true);
    setImportError(null);
    setImportResult(null);

    try {
      const payload = await parseImportPayload(importFile);
      const response = await apiClient.importDishes(payload);
      setImportResult(response);
    } catch (error) {
      if (error instanceof ApiError) {
        const body = error.body;
        const message = typeof body === "string" ? body : error.message;
        setImportError(message);
        if (body && typeof body === "object" && "failures" in body) {
          const response = body as {
            attempted?: number;
            imported?: number;
            skipped?: number;
            failed?: number;
            failures?: { dishName?: string | null; reason: string; ingredientName?: string | null }[];
          };
          setImportResult({
            attempted: response.attempted ?? 0,
            imported: response.imported ?? 0,
            skipped: response.skipped ?? 0,
            failed: response.failed ?? 0,
            failures: response.failures ?? [],
          });
        }
      } else if (error instanceof Error) {
        setImportError(error.message);
      } else {
        setImportError("Unexpected error while importing dishes.");
      }
    } finally {
      setIsImporting(false);
    }
  };

  return (
    <div className="min-h-screen w-full p-6 sm:p-8">
      <div className="flex flex-wrap items-start gap-6">
        <Sidebar />
        <main className="flex-1 min-w-[280px] space-y-6">
          <header className="flex flex-wrap items-start justify-between gap-4">
            <div className="flex items-center gap-3">
              <span className="grid h-12 w-12 place-items-center rounded-2xl bg-[#f0f4ee] text-[#2f6b4f]">
                <DishIcon className="h-5 w-5" />
              </span>
              <div>
                <h1 className="text-2xl font-semibold text-[#1f2a22]">
                  Dishes
                </h1>
                <p className="text-sm text-[#6c7a70]">
                  Manage your recipe collection
                </p>
              </div>
            </div>
            <div className="flex flex-wrap items-center gap-3">
              <button
                type="button"
                onClick={() => {
                  closeModal();
                  setIsImportOpen(true);
                }}
                className="inline-flex items-center gap-2 rounded-full border border-[#d6e0d2] bg-white px-4 py-2 text-sm font-semibold text-[#3b4c42] transition hover:bg-[#f3f6ef]"
              >
                <ImportIcon className="h-4 w-4" />
                Import dishes
              </button>
              <button
                type="button"
                onClick={() => {
                  setSelectedDish(null);
                  setIsCreateOpen(true);
                }}
                className="inline-flex items-center gap-2 rounded-full bg-[#2f6b4f] px-4 py-2 text-sm font-semibold text-white shadow-[0_12px_24px_-18px_rgba(32,78,54,0.9)] transition hover:bg-[#2a5c46]"
              >
                <PlusIcon className="h-4 w-4" />
                Add dish
              </button>
            </div>
          </header>

          <div className="flex items-center gap-3 rounded-2xl border border-[#e1e8dc] bg-white/80 px-4 py-3 shadow-[0_10px_30px_-26px_rgba(30,60,40,0.4)]">
            <SearchIcon className="h-4 w-4 text-[#7a887f]" />
            <input
              type="text"
              aria-label="Search dishes"
              placeholder="Search dishes by name or category..."
              className="w-full bg-transparent text-sm text-[#2e3b33] placeholder:text-[#9aa69f] focus:outline-none"
            />
          </div>

          <section className="grid gap-5 lg:grid-cols-2 xl:grid-cols-3">
            {dishes.map((dish) => {
              const previewIngredients = dish.ingredients.slice(0, 3);
              const remainingCount = dish.ingredients.length - previewIngredients.length;

              return (
                <article
                  key={dish.id}
                  className="rounded-2xl border border-[#e3eadf] bg-white/80 p-5 shadow-[0_16px_40px_-30px_rgba(35,60,42,0.35)]"
                >
                  <div className="flex items-start justify-between gap-4">
                    <div>
                      <h2 className="text-base font-semibold text-[#1f2a22]">
                        {dish.name}
                      </h2>
                      <span className="mt-2 inline-flex rounded-full bg-[#edf1ea] px-3 py-1 text-xs font-semibold text-[#4f5f55]">
                        {dish.cuisine}
                      </span>
                    </div>
                    <div className="flex items-center gap-2">
                      <button
                        type="button"
                        aria-label={`Edit ${dish.name}`}
                        onClick={() => {
                          setIsCreateOpen(false);
                          setSelectedDish(dish);
                        }}
                        className="grid h-9 w-9 place-items-center rounded-full border border-[#e3eadf] text-[#6e7c72] transition hover:bg-[#f4f7f1]"
                      >
                        <EditIcon className="h-4 w-4" />
                      </button>
                      <button
                        type="button"
                        aria-label={`Delete ${dish.name}`}
                        className="grid h-9 w-9 place-items-center rounded-full border border-[#f0dada] text-[#d76b6b] transition hover:bg-[#fbeeee]"
                      >
                        <TrashIcon className="h-4 w-4" />
                      </button>
                    </div>
                  </div>

                  <div className="mt-4 flex flex-wrap items-center gap-3 text-xs font-semibold text-[#6c7a70]">
                    <span className="flex items-center gap-2">
                      <TimerIcon className="h-4 w-4 text-[#2f6b4f]" />
                      Prep: {formatMinutes(dish.prepMinutes)}
                    </span>
                    <span className="flex items-center gap-2">
                      <PanSmallIcon className="h-4 w-4 text-[#2f6b4f]" />
                      Cook: {formatMinutes(dish.cookMinutes)}
                    </span>
                    <span className="flex items-center gap-2">
                      <PeopleIcon className="h-4 w-4 text-[#2f6b4f]" />
                      Serves: {dish.serves}
                    </span>
                  </div>

                  <div className="mt-4">
                    <div className="text-xs font-semibold uppercase tracking-[0.18em] text-[#7a887f]">
                      Ingredients
                    </div>
                    <ul className="mt-3 space-y-2 text-sm text-[#3d4c43]">
                      {previewIngredients.map((ingredient) => (
                        <li
                          key={ingredient.id}
                          className="flex items-center gap-2"
                        >
                          <span className="h-2 w-2 rounded-full bg-[#9bb09f]" />
                          {ingredient.label}
                        </li>
                      ))}
                      {remainingCount > 0 ? (
                        <li className="text-xs font-semibold text-[#6d7b72]">
                          + {remainingCount} more...
                        </li>
                      ) : null}
                    </ul>
                  </div>
                </article>
              );
            })}
          </section>
        </main>
      </div>

      <Modal
        isOpen={isModalOpen}
        onClose={closeModal}
        title={isEditMode ? "Edit Dish" : "Add Dish"}
        description={
          isEditMode
            ? "Update the dish details below"
            : "Add the dish details below"
        }
        maxWidthClassName="max-w-2xl"
        footer={
          <>
            <button
              type="button"
              onClick={closeModal}
              className="inline-flex items-center justify-center rounded-xl border border-[#dfe6da] bg-white px-4 py-2 text-sm font-semibold text-[#3f4b43] transition hover:bg-[#f3f6ef]"
            >
              Cancel
            </button>
            <button
              type="button"
              className="inline-flex items-center justify-center rounded-xl bg-[#2f6b4f] px-4 py-2 text-sm font-semibold text-white shadow-[0_12px_22px_-18px_rgba(30,68,48,0.8)] transition hover:bg-[#2a5c46]"
            >
              {isEditMode ? "Update Dish" : "Create Dish"}
            </button>
          </>
        }
      >
        <div className="mt-6 grid gap-4">
          <div className="grid gap-4 sm:grid-cols-2">
            <label className="grid gap-2 text-sm font-semibold text-[#3f4b43]">
              Dish Name
              <input
                type="text"
                defaultValue={activeDish.name}
                placeholder="Dish name"
                className="rounded-xl border border-[#e1e7dd] bg-white px-3 py-2 text-sm text-[#2e3b33] focus:outline-none focus:ring-2 focus:ring-[#2f6b4f]/30"
              />
            </label>
            <label className="grid gap-2 text-sm font-semibold text-[#3f4b43]">
              Category
              <input
                type="text"
                defaultValue={activeDish.cuisine}
                placeholder="Cuisine type"
                className="rounded-xl border border-[#e1e7dd] bg-white px-3 py-2 text-sm text-[#2e3b33] focus:outline-none focus:ring-2 focus:ring-[#2f6b4f]/30"
              />
            </label>
          </div>

          <div className="grid gap-8 sm:grid-cols-2 lg:grid-cols-3">
            <label className="grid gap-2 text-sm font-semibold text-[#3f4b43]">
              Prep Time (min)
              <input
                type="number"
                defaultValue={isEditMode ? activeDish.prepMinutes : ""}
                placeholder="0"
                className="rounded-xl border border-[#e1e7dd] bg-white px-3 py-2 text-sm text-[#2e3b33] focus:outline-none focus:ring-2 focus:ring-[#2f6b4f]/30"
              />
            </label>
            <label className="grid gap-2 text-sm font-semibold text-[#3f4b43]">
              Cook Time (min)
              <input
                type="number"
                defaultValue={isEditMode ? activeDish.cookMinutes : ""}
                placeholder="0"
                className="rounded-xl border border-[#e1e7dd] bg-white px-3 py-2 text-sm text-[#2e3b33] focus:outline-none focus:ring-2 focus:ring-[#2f6b4f]/30"
              />
            </label>
            <label className="grid gap-2 text-sm font-semibold text-[#3f4b43]">
              Servings
              <input
                type="number"
                defaultValue={isEditMode ? activeDish.serves : ""}
                placeholder="0"
                className="rounded-xl border border-[#e1e7dd] bg-white px-3 py-2 text-sm text-[#2e3b33] focus:outline-none focus:ring-2 focus:ring-[#2f6b4f]/30"
              />
            </label>
          </div>

          <label className="grid gap-2 text-sm font-semibold text-[#3f4b43]">
            Instructions (optional)
            <textarea
              defaultValue={activeDish.instructions}
              placeholder="Write the steps to cook the dish..."
              rows={4}
              className="rounded-xl border border-[#e1e7dd] bg-white px-3 py-2 text-sm text-[#2e3b33] focus:outline-none focus:ring-2 focus:ring-[#2f6b4f]/30"
            />
          </label>

          <div>
            <div className="text-sm font-semibold text-[#3f4b43]">
              Ingredients
            </div>
            <div className="mt-3 grid gap-3 sm:grid-cols-[1fr_140px_auto]">
              <div className="flex items-center gap-3 rounded-xl border border-[#e1e7dd] bg-white px-3 py-2 text-sm text-[#6f7c73]">
                <span>Select an ingredient</span>
                <ChevronDownIcon className="ml-auto h-4 w-4" />
              </div>
              <input
                type="text"
                placeholder="Amount"
                className="rounded-xl border border-[#e1e7dd] bg-white px-3 py-2 text-sm text-[#2e3b33] focus:outline-none focus:ring-2 focus:ring-[#2f6b4f]/30"
              />
              <button
                type="button"
                className="inline-flex items-center justify-center rounded-xl border border-[#dfe6da] bg-white px-4 py-2 text-sm font-semibold text-[#3f4b43] transition hover:bg-[#f3f6ef]"
              >
                Add
              </button>
            </div>

            <ul className="mt-4 space-y-3 text-sm text-[#3d4c43]">
              {activeDish.ingredients.length > 0 ? (
                activeDish.ingredients.map((ingredient) => (
                  <li
                    key={ingredient.id}
                    className="flex items-center justify-between gap-3"
                  >
                    <span className="flex items-center gap-2">
                      <span className="h-2 w-2 rounded-full bg-[#9bb09f]" />
                      {ingredient.label}
                    </span>
                    <button
                      type="button"
                      aria-label={`Remove ${ingredient.label}`}
                      className="grid h-8 w-8 place-items-center rounded-full border border-[#f0dada] text-[#d76b6b] transition hover:bg-[#fbeeee]"
                    >
                      <TrashIcon className="h-4 w-4" />
                    </button>
                  </li>
                ))
              ) : (
                <li className="text-sm text-[#8a968f]">
                  No ingredients added yet.
                </li>
              )}
            </ul>
          </div>
        </div>
      </Modal>

      <Modal
        isOpen={isImportOpen}
        onClose={closeImportModal}
        title="Import dishes"
        description="Upload a JSON file to import dishes and ingredients."
        maxWidthClassName="max-w-xl"
        footer={
          <>
            <button
              type="button"
              onClick={closeImportModal}
              className="inline-flex items-center justify-center rounded-xl border border-[#dfe6da] bg-white px-4 py-2 text-sm font-semibold text-[#3f4b43] transition hover:bg-[#f3f6ef]"
            >
              Cancel
            </button>
            <button
              type="button"
              onClick={handleImport}
              disabled={!importFile || isImporting}
              className="inline-flex items-center justify-center rounded-xl bg-[#2f6b4f] px-4 py-2 text-sm font-semibold text-white shadow-[0_12px_22px_-18px_rgba(30,68,48,0.8)] transition hover:bg-[#2a5c46] disabled:cursor-not-allowed disabled:bg-[#98b6a6]"
            >
              {isImporting ? "Importing..." : "Import dishes"}
            </button>
          </>
        }
      >
        <div className="mt-6 grid gap-4">
          <label className="grid gap-2 text-sm font-semibold text-[#3f4b43]">
            JSON file
            <input
              type="file"
              accept="application/json,.json"
              onChange={(event) => {
                const file = event.target.files?.[0] ?? null;
                setImportFile(file);
                setImportError(null);
                setImportResult(null);
              }}
              className="rounded-xl border border-[#e1e7dd] bg-white px-3 py-2 text-sm text-[#2e3b33] file:mr-3 file:rounded-lg file:border-0 file:bg-[#edf1ea] file:px-3 file:py-1 file:text-xs file:font-semibold file:text-[#3f4b43]"
            />
          </label>

          {importFile ? (
            <div className="text-xs font-semibold text-[#5a675e]">
              Selected: {importFile.name}
            </div>
          ) : null}

          {importError ? (
            <div className="rounded-xl border border-[#f0dada] bg-[#fff5f5] px-4 py-3 text-sm text-[#b14a4a]">
              {importError}
            </div>
          ) : null}

          {importResult ? (
            <div className="rounded-2xl border border-[#e3eadf] bg-[#f6faf5] px-4 py-4 text-sm text-[#2e3b33]">
              <div className="grid gap-1 text-sm font-semibold text-[#2f6b4f]">
                Import summary
              </div>
              <div className="mt-2 grid gap-2 text-xs font-semibold text-[#5a675e] sm:grid-cols-2">
                <div>Attempted: {importResult.attempted}</div>
                <div>Imported: {importResult.imported}</div>
                <div>Skipped: {importResult.skipped}</div>
                <div>Failed: {importResult.failed}</div>
              </div>
              {importResult.failures.length > 0 ? (
                <div className="mt-4">
                  <div className="text-xs font-semibold uppercase tracking-[0.18em] text-[#7a887f]">
                    Failures
                  </div>
                  <ul className="mt-3 space-y-2 text-sm text-[#a04242]">
                    {importResult.failures.map((failure, index) => (
                      <li key={`${failure.dishName ?? "dish"}-${index}`}>
                        <span className="font-semibold">
                          {failure.dishName ?? "Unknown dish"}
                        </span>
                        {failure.ingredientName ? (
                          <span className="text-[#7c3c3c]">
                            {" "}
                            ({failure.ingredientName})
                          </span>
                        ) : null}
                        : {failure.reason}
                      </li>
                    ))}
                  </ul>
                </div>
              ) : null}
            </div>
          ) : null}
        </div>
      </Modal>
    </div>
  );
}

function DishIcon({ className }: { className?: string }) {
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

function SearchIcon({ className }: { className?: string }) {
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
      <circle cx="11" cy="11" r="6.5" />
      <path d="M16 16l4 4" />
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
      strokeWidth="1.8"
      strokeLinecap="round"
      className={className}
    >
      <path d="M12 5v14M5 12h14" />
    </svg>
  );
}

function ImportIcon({ className }: { className?: string }) {
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
      <path d="M12 3v12" />
      <path d="m7 8 5-5 5 5" />
      <path d="M5 15v4a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2v-4" />
    </svg>
  );
}

function EditIcon({ className }: { className?: string }) {
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
      <path d="M5 19h4l10-10-4-4L5 15v4Z" />
      <path d="M13 5l4 4" />
    </svg>
  );
}

function TrashIcon({ className }: { className?: string }) {
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
      <path d="M4 7h16" />
      <path d="M9 7V5h6v2" />
      <path d="m7 7 1 12h8l1-12" />
      <path d="M10 11v6M14 11v6" />
    </svg>
  );
}

function TimerIcon({ className }: { className?: string }) {
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
      <circle cx="12" cy="13" r="7" />
      <path d="M12 13V9" />
      <path d="M9 3h6" />
    </svg>
  );
}

function PanSmallIcon({ className }: { className?: string }) {
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
      <path d="M6 12h9a3.5 3.5 0 0 1 0 7H7.5A1.5 1.5 0 0 1 6 17.5V12Z" />
      <path d="M15 12h4a1.5 1.5 0 0 0 0-3h-4" />
    </svg>
  );
}

function PeopleIcon({ className }: { className?: string }) {
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
      <path d="M8.5 11.5a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7Z" />
      <path d="M15.5 12.5a3 3 0 1 0 0-6 3 3 0 0 0 0 6Z" />
      <path d="M3.5 19.5a5.5 5.5 0 0 1 10 0" />
      <path d="M14 19.5a4.5 4.5 0 0 1 6 0" />
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
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
    >
      <path d="M6 9l6 6 6-6" />
    </svg>
  );
}
