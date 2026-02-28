"use client";

import { useMemo, useState, useCallback, useEffect } from "react";
import Sidebar from "../components/Sidebar";
import { apiClient, ApiError } from "../../lib/api/client";
import type { RecipeSuggestion, RecipeInstruction } from "../../lib/api/models/recipes";

type TabKey = "instructions" | "ai";

export default function RecipesPage() {
  const [activeTab, setActiveTab] = useState<TabKey>("ai");
  const [prompt, setPrompt] = useState("");
  const [suggestions, setSuggestions] = useState<RecipeSuggestion[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [savingId, setSavingId] = useState<string | null>(null);
  const [savedIds, setSavedIds] = useState<Set<string>>(new Set());
  const [saveError, setSaveError] = useState<string | null>(null);

  const [recipes, setRecipes] = useState<RecipeInstruction[]>([]);
  const [recipesLoading, setRecipesLoading] = useState(false);
  const [recipesError, setRecipesError] = useState<string | null>(null);
  const [selectedDishId, setSelectedDishId] = useState<string | null>(null);

  const selectedRecipe = useMemo(
    () => recipes.find((recipe) => recipe.dishId === selectedDishId) ?? null,
    [recipes, selectedDishId],
  );

  useEffect(() => {
    const loadRecipes = async () => {
      setRecipesLoading(true);
      setRecipesError(null);

      try {
        const response = await apiClient.getRecipeInstructions();
        setRecipes(response.recipes);
      } catch (err) {
        if (err instanceof ApiError) {
          const body = err.body as { message?: string } | undefined;
          setRecipesError(body?.message ?? `Failed to load recipes: ${err.message}`);
        } else {
          setRecipesError("An unexpected error occurred while loading recipes.");
        }
      } finally {
        setRecipesLoading(false);
      }
    };

    loadRecipes();
  }, []);

  const showAiEmptyState = useMemo(
    () => activeTab === "ai" && suggestions.length === 0 && !isLoading && !error,
    [activeTab, suggestions.length, isLoading, error],
  );

  const handleGetSuggestions = useCallback(async () => {
    if (!prompt.trim()) {
      return;
    }

    setIsLoading(true);
    setError(null);
    setSuggestions([]);
    setSavedIds(new Set());
    setSaveError(null);

    try {
      const response = await apiClient.getRecipeSuggestions({
        prompt: prompt.trim(),
        maxSuggestions: 5,
      });
      setSuggestions(response.suggestions);
    } catch (err) {
      if (err instanceof ApiError) {
        const body = err.body as { message?: string } | undefined;
        setError(body?.message ?? `Failed to get suggestions: ${err.message}`);
      } else {
        setError("An unexpected error occurred while fetching suggestions.");
      }
    } finally {
      setIsLoading(false);
    }
  }, [prompt]);

  const handleSaveDish = useCallback(async (suggestion: RecipeSuggestion) => {
    setSavingId(suggestion.id);
    setSaveError(null);

    try {
      await apiClient.saveRecipeFromSuggestion({
        title: suggestion.title,
        summary: suggestion.summary,
        estimatedTotalMinutes: suggestion.estimatedTotalMinutes,
      });
      setSavedIds((prev) => new Set(prev).add(suggestion.id));
    } catch (err) {
      if (err instanceof ApiError) {
        const body = err.body as { message?: string } | undefined;
        setSaveError(body?.message ?? `Failed to save dish: ${err.message}`);
      } else {
        setSaveError("An unexpected error occurred while saving the dish.");
      }
    } finally {
      setSavingId(null);
    }
  }, []);

  return (
    <div className="min-h-screen w-full p-6 sm:p-8">
      <div className="flex flex-wrap items-start gap-6">
        <Sidebar />
        <main className="min-w-[280px] flex-1 space-y-6">
          <header className="space-y-3">
            <div className="flex items-center gap-3">
              <span className="grid h-12 w-12 place-items-center rounded-2xl bg-[#eef4ee] text-[#2f6b4f]">
                <BookIcon className="h-5 w-5" />
              </span>
              <div>
                <h1 className="text-2xl font-semibold text-[#1f2a22]">
                  Recipes &amp; Instructions
                </h1>
                <p className="text-sm text-[#6c7a70]">
                  View detailed cooking instructions and get AI-powered suggestions
                </p>
              </div>
            </div>

            <div className="inline-flex rounded-2xl bg-[#e8ebe6] p-1">
              <button
                type="button"
                onClick={() => setActiveTab("instructions")}
                className={`min-w-48 rounded-xl px-4 py-2 text-sm font-semibold transition ${
                  activeTab === "instructions"
                    ? "bg-white text-[#1f2a22] shadow-[0_10px_20px_-16px_rgba(23,43,30,0.7)]"
                    : "text-[#4f5f55]"
                }`}
              >
                Instructions
              </button>
              <button
                type="button"
                onClick={() => setActiveTab("ai")}
                className={`min-w-48 rounded-xl border px-4 py-2 text-sm font-semibold transition ${
                  activeTab === "ai"
                    ? "border-[#4cb287] bg-white text-[#1f2a22] shadow-[0_10px_20px_-16px_rgba(23,43,30,0.7)]"
                    : "border-transparent text-[#4f5f55]"
                }`}
              >
                AI Suggestions
              </button>
            </div>
          </header>

          {activeTab === "instructions" ? (
            <section className="rounded-[18px] border border-[#e1e8dc] bg-white/80 p-6 shadow-[0_20px_48px_-36px_rgba(30,60,40,0.4)]">
              <div className="space-y-4">
                <div className="space-y-1">
                  <div className="flex items-center gap-2 text-xl font-semibold text-[#1f2a22]">
                    <BookIcon className="h-5 w-5 text-[#2f6b4f]" />
                    Cooking Instructions
                  </div>
                  <p className="text-sm text-[#6c7a70]">
                    Select a dish to view step-by-step cooking instructions
                  </p>
                </div>

                {recipesLoading ? (
                  <div className="flex min-h-[420px] flex-col items-center justify-center gap-4 text-center text-[#7a857d]">
                    <LoadingIcon className="h-10 w-10 animate-spin text-[#2f6b4f]" />
                    <p className="text-sm font-medium">Loading recipes...</p>
                  </div>
                ) : recipesError ? (
                  <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
                    {recipesError}
                  </div>
                ) : (
                  <>
                    <div className="relative">
                      <select
                        value={selectedDishId ?? ""}
                        onChange={(event) => setSelectedDishId(event.target.value || null)}
                        className="w-full appearance-none rounded-2xl border border-[#dfe7d7] bg-white px-4 py-3 pr-10 text-sm text-[#1f2a22] shadow-[0_10px_20px_-18px_rgba(30,60,40,0.4)] outline-none focus:border-[#7fc3a5]"
                      >
                        <option value="">Select a dish...</option>
                        {recipes.map((recipe) => (
                          <option key={recipe.dishId} value={recipe.dishId}>
                            {recipe.dishName}
                          </option>
                        ))}
                      </select>
                      <ChevronDownIcon className="pointer-events-none absolute right-3 top-1/2 h-5 w-5 -translate-y-1/2 text-[#6c7a70]" />
                    </div>

                    <div className="rounded-2xl border border-[#e6ece2] bg-[#fcfdfb] p-4">
                      {!selectedRecipe ? (
                        <div className="flex min-h-[320px] flex-col items-center justify-center gap-4 text-center text-[#7a857d]">
                          <BookIcon className="h-12 w-12 text-[#b3bab3]" />
                          <p className="text-sm font-medium">
                            Select a dish above to view its cooking instructions
                          </p>
                        </div>
                      ) : (
                        <div className="space-y-6">
                          <div className="space-y-2">
                            <h3 className="text-lg font-semibold text-[#1f2a22]">
                              {selectedRecipe.dishName}
                            </h3>
                            {selectedRecipe.summary && (
                              <p className="text-sm text-[#4f5f55]">{selectedRecipe.summary}</p>
                            )}
                            <div className="flex flex-wrap gap-4 text-xs text-[#6c7a70]">
                              {selectedRecipe.totalMinutes && (
                                <span className="inline-flex items-center gap-1">
                                  <ClockIcon className="h-3.5 w-3.5" />
                                  ~{selectedRecipe.totalMinutes} min
                                </span>
                              )}
                              {selectedRecipe.servings && (
                                <span className="inline-flex items-center gap-1">
                                  <ServingsIcon className="h-3.5 w-3.5" />
                                  {selectedRecipe.servings} servings
                                </span>
                              )}
                            </div>
                          </div>

                          <div className="space-y-3">
                            <h4 className="text-sm font-semibold text-[#1f2a22]">Ingredients</h4>
                            {selectedRecipe.ingredients.length > 0 ? (
                              <ul className="space-y-2">
                                {selectedRecipe.ingredients.map((ingredient) => (
                                  <li
                                    key={ingredient.ingredientId}
                                    className="flex items-center gap-3 rounded-xl border border-[#dfe7d7] bg-white px-4 py-3"
                                  >
                                    <span className="grid h-6 w-6 flex-shrink-0 place-items-center rounded-full bg-[#eef4ee]">
                                      <IngredientIcon className="h-3.5 w-3.5 text-[#2f6b4f]" />
                                    </span>
                                    <div className="flex flex-1 items-center justify-between gap-2">
                                      <span className="text-sm text-[#1f2a22]">
                                        {ingredient.name}
                                        {ingredient.note && (
                                          <span className="ml-1 text-[#6c7a70]">
                                            ({ingredient.note})
                                          </span>
                                        )}
                                      </span>
                                      <span className="text-sm font-medium text-[#4f5f55]">
                                        {ingredient.quantity} {ingredient.unit.toLowerCase()}
                                      </span>
                                    </div>
                                  </li>
                                ))}
                              </ul>
                            ) : (
                              <p className="text-sm text-[#6c7a70]">No ingredients listed.</p>
                            )}
                          </div>

                          <div className="space-y-3">
                            <h4 className="text-sm font-semibold text-[#1f2a22]">Steps</h4>
                            <ol className="space-y-3">
                              {selectedRecipe.steps.map((step) => (
                                <li
                                  key={step.order}
                                  className="flex gap-3 rounded-xl border border-[#dfe7d7] bg-white p-4"
                                >
                                  <span className="grid h-6 w-6 flex-shrink-0 place-items-center rounded-full bg-[#eef4ee] text-xs font-semibold text-[#2f6b4f]">
                                    {step.order}
                                  </span>
                                  <div className="space-y-1">
                                    {step.heading && (
                                      <p className="text-sm font-medium text-[#1f2a22]">
                                        {step.heading}
                                      </p>
                                    )}
                                    <p className="text-sm text-[#4f5f55]">{step.description}</p>
                                  </div>
                                </li>
                              ))}
                            </ol>
                          </div>
                        </div>
                      )}
                    </div>
                  </>
                )}
              </div>
            </section>
          ) : (
            <section className="rounded-[18px] border border-[#e1e8dc] bg-white/80 p-6 shadow-[0_20px_48px_-36px_rgba(30,60,40,0.4)]">
              <div className="space-y-4">
                <div className="space-y-1">
                  <div className="flex items-center gap-2 text-xl font-semibold text-[#1f2a22]">
                    <SparkleIcon className="h-5 w-5 text-[#2f6b4f]" />
                    AI Recipe Suggestions
                  </div>
                  <p className="text-sm text-[#6c7a70]">
                    Describe what you&apos;re looking for and get personalized recipe ideas
                  </p>
                </div>

                <textarea
                  value={prompt}
                  onChange={(event) => setPrompt(event.target.value)}
                  rows={5}
                  placeholder="E.g., I want a healthy vegetarian dinner that takes less than 30 minutes..."
                  className="w-full resize-none rounded-2xl border border-[#dfe7d7] bg-white px-4 py-3 text-sm text-[#1f2a22] shadow-[0_10px_20px_-18px_rgba(30,60,40,0.4)] outline-none placeholder:text-[#8a968d] focus:border-[#7fc3a5]"
                />

                <button
                  type="button"
                  onClick={handleGetSuggestions}
                  disabled={isLoading || !prompt.trim()}
                  className="inline-flex w-full items-center justify-center gap-2 rounded-lg bg-[#2f6b4f] px-4 py-2.5 text-sm font-semibold text-white shadow-[0_12px_24px_-18px_rgba(32,78,54,0.9)] transition hover:bg-[#2a5c46] disabled:cursor-not-allowed disabled:opacity-50"
                >
                  {isLoading ? (
                    <>
                      <LoadingIcon className="h-4 w-4 animate-spin" />
                      Getting suggestions...
                    </>
                  ) : (
                    <>
                      <SparkleIcon className="h-4 w-4" />
                      Get AI Suggestions
                    </>
                  )}
                </button>

                {error && (
                  <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
                    {error}
                  </div>
                )}

                {saveError && (
                  <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
                    {saveError}
                  </div>
                )}

                <div className="rounded-2xl border border-[#e6ece2] bg-[#fcfdfb] p-4">
                  {showAiEmptyState ? (
                    <div className="flex min-h-[320px] flex-col items-center justify-center gap-4 text-center text-[#7a857d]">
                      <SparkleIcon className="h-12 w-12 text-[#b3bab3]" />
                      <p className="text-sm font-medium">
                        Enter a prompt above to get AI-powered recipe suggestions
                      </p>
                    </div>
                  ) : isLoading ? (
                    <div className="flex min-h-[320px] flex-col items-center justify-center gap-4 text-center text-[#7a857d]">
                      <LoadingIcon className="h-10 w-10 animate-spin text-[#2f6b4f]" />
                      <p className="text-sm font-medium">Generating suggestions...</p>
                    </div>
                  ) : suggestions.length > 0 ? (
                    <div className="space-y-4">
                      {suggestions.map((suggestion) => {
                        const isSaved = savedIds.has(suggestion.id);
                        const isSaving = savingId === suggestion.id;

                        return (
                          <div
                            key={suggestion.id}
                            className="rounded-xl border border-[#dfe7d7] bg-white p-4 shadow-sm transition hover:shadow-md"
                          >
                            <div className="flex items-start justify-between gap-4">
                              <div className="min-w-0 flex-1 space-y-2">
                                <h3 className="text-lg font-semibold text-[#1f2a22]">
                                  {suggestion.title}
                                </h3>
                                <p className="text-sm text-[#4f5f55]">{suggestion.summary}</p>
                                <div className="flex flex-wrap gap-3 text-xs text-[#6c7a70]">
                                  {suggestion.estimatedTotalMinutes && (
                                    <span className="inline-flex items-center gap-1">
                                      <ClockIcon className="h-3.5 w-3.5" />
                                      ~{suggestion.estimatedTotalMinutes} min
                                    </span>
                                  )}
                                  {suggestion.reason && (
                                    <span className="italic">{suggestion.reason}</span>
                                  )}
                                </div>
                              </div>
                              <button
                                type="button"
                                onClick={() => handleSaveDish(suggestion)}
                                disabled={isSaving || isSaved}
                                className={`flex-shrink-0 rounded-lg px-4 py-2 text-sm font-semibold transition ${
                                  isSaved
                                    ? "cursor-default bg-[#e8f5e9] text-[#2e7d32]"
                                    : "bg-[#2f6b4f] text-white shadow-[0_8px_16px_-12px_rgba(32,78,54,0.7)] hover:bg-[#2a5c46] disabled:cursor-not-allowed disabled:opacity-50"
                                }`}
                              >
                                {isSaving ? (
                                  <span className="inline-flex items-center gap-2">
                                    <LoadingIcon className="h-4 w-4 animate-spin" />
                                    Saving...
                                  </span>
                                ) : isSaved ? (
                                  <span className="inline-flex items-center gap-1">
                                    <CheckIcon className="h-4 w-4" />
                                    Saved
                                  </span>
                                ) : (
                                  "Save Dish"
                                )}
                              </button>
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  ) : (
                    <div className="flex min-h-[320px] flex-col items-center justify-center gap-4 text-center text-[#7a857d]">
                      <SparkleIcon className="h-12 w-12 text-[#b3bab3]" />
                      <p className="text-sm font-medium">
                        No suggestions found. Try a different prompt.
                      </p>
                    </div>
                  )}
                </div>
              </div>
            </section>
          )}
        </main>
      </div>
    </div>
  );
}

type IconProps = {
  className?: string;
};

function BookIcon({ className }: IconProps) {
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
      <path d="M4 5.5A2.5 2.5 0 0 1 6.5 3H20v16H6.5A2.5 2.5 0 0 0 4 21z" />
      <path d="M4 5.5v15" />
      <path d="M12 3v16" />
    </svg>
  );
}

function SparkleIcon({ className }: IconProps) {
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
      <path d="m12 3 1.6 4.4L18 9l-4.4 1.6L12 15l-1.6-4.4L6 9l4.4-1.6Z" />
      <path d="m19 4 .6 1.6 1.6.6-1.6.6L19 8.4l-.6-1.6-1.6-.6 1.6-.6Z" />
      <path d="m5 15 .5 1.2L6.8 17l-1.3.5L5 18.8l-.5-1.3L3.2 17l1.3-.8Z" />
    </svg>
  );
}

function LoadingIcon({ className }: IconProps) {
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
      <path d="M21 12a9 9 0 1 1-6.219-8.56" />
    </svg>
  );
}

function ClockIcon({ className }: IconProps) {
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
      <circle cx="12" cy="12" r="10" />
      <polyline points="12 6 12 12 16 14" />
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
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
    >
      <polyline points="20 6 9 17 4 12" />
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
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
    >
      <polyline points="6 9 12 15 18 9" />
    </svg>
  );
}

function ServingsIcon({ className }: IconProps) {
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
      <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
      <circle cx="9" cy="7" r="4" />
      <path d="M23 21v-2a4 4 0 0 0-3-3.87" />
      <path d="M16 3.13a4 4 0 0 1 0 7.75" />
    </svg>
  );
}

function IngredientIcon({ className }: IconProps) {
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
      <path d="M12 2L2 7l10 5 10-5-10-5z" />
      <path d="M2 17l10 5 10-5" />
      <path d="M2 12l10 5 10-5" />
    </svg>
  );
}
