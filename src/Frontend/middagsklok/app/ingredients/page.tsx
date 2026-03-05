"use client";

import { useEffect, useMemo, useState } from "react";
import Sidebar from "../components/Sidebar";
import Modal from "../components/Modal";
import { ApiError, apiClient } from "../../lib/api/client";
import type {
  IngredientErrorResponse,
  IngredientOverview,
  IngredientValidationError,
} from "../../lib/api/models/ingredients";
import { useIngredientsMetadata } from "../components/IngredientsMetadataProvider";

type Ingredient = IngredientOverview;

const emptyIngredient = {
  id: "new-ingredient",
  name: "",
  category: "" as Ingredient["category"],
  defaultUnit: "" as Ingredient["defaultUnit"],
  usedIn: 0,
};

const formatUsage = (count: number) => `${count} dish(es)`;

export default function IngredientsPage() {
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedCategory, setSelectedCategory] = useState("all");
  const [selectedIngredient, setSelectedIngredient] =
    useState<Ingredient | null>(null);
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [ingredients, setIngredients] = useState<Ingredient[]>([]);
  const [formName, setFormName] = useState("");
  const [formCategory, setFormCategory] = useState<Ingredient["category"] | "">(
    "",
  );
  const [formDefaultUnit, setFormDefaultUnit] = useState<
    Ingredient["defaultUnit"] | ""
  >("");
  const [validationErrors, setValidationErrors] = useState<
    IngredientValidationError[]
  >([]);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<Ingredient | null>(null);
  const [deleteError, setDeleteError] = useState<string | null>(null);
  const [deleteErrors, setDeleteErrors] = useState<IngredientValidationError[]>(
    [],
  );
  const [isDeleting, setIsDeleting] = useState(false);
  const { categories, units } = useIngredientsMetadata();

  useEffect(() => {
    let isActive = true;

    const loadIngredients = async () => {
      try {
        const response = await apiClient.getIngredients();
        if (isActive) {
          setIngredients(response.ingredients ?? []);
        }
      } catch (error) {
        if (error instanceof ApiError) {
          console.error("Failed to load ingredients:", error.body ?? error.message);
        } else if (error instanceof Error) {
          console.error("Failed to load ingredients:", error.message);
        } else {
          console.error("Failed to load ingredients.");
        }
      }
    };

    void loadIngredients();

    return () => {
      isActive = false;
    };
  }, []);

  const visibleIngredients = useMemo(() => {
    const query = searchQuery.trim().toLowerCase();

    return ingredients.filter((ingredient) => {
      const matchesSearch = !query
        || ingredient.name.toLowerCase().includes(query);
      const matchesCategory =
        selectedCategory === "all"
        || ingredient.category === selectedCategory;

      return matchesSearch && matchesCategory;
    });
  }, [ingredients, searchQuery, selectedCategory]);

  const categoryOptions = useMemo(() => {
    const fallback = Array.from(
      new Set(ingredients.map((ingredient) => ingredient.category)),
    ).map((value) => ({
      value,
      label: value,
    }));

    const categoryItems = categories.length > 0 ? categories : fallback;

    return [
      { value: "all", label: "All Categories" },
      ...categoryItems,
    ];
  }, [categories, ingredients]);

  const categoryLabels = useMemo(
    () => new Map(categories.map((category) => [category.value, category.label])),
    [categories],
  );

  const unitLabels = useMemo(
    () => new Map(units.map((unit) => [unit.value, unit.label])),
    [units],
  );

  const modalCategoryOptions = useMemo(() => {
    if (categories.length > 0) {
      return categories;
    }

    return Array.from(
      new Set(ingredients.map((ingredient) => ingredient.category)),
    ).map((value) => ({ value, label: value }));
  }, [categories, ingredients]);

  const unitOptions = useMemo(() => {
    if (units.length > 0) {
      return units;
    }

    return Array.from(
      new Set(ingredients.map((ingredient) => ingredient.defaultUnit)),
    ).map((value) => ({ value, label: value }));
  }, [ingredients, units]);

  const totalIngredients = ingredients.length;
  const usedIngredients = ingredients.filter(
    (ingredient) => ingredient.usedIn > 0,
  ).length;

  const isModalOpen = isCreateOpen || selectedIngredient !== null;
  const isEditMode = selectedIngredient !== null;
  const activeIngredient = selectedIngredient ?? emptyIngredient;

  useEffect(() => {
    if (!isModalOpen) {
      return;
    }

    setFormName(activeIngredient.name ?? "");
    setFormCategory(activeIngredient.category ?? "");
    setFormDefaultUnit(activeIngredient.defaultUnit ?? "");
    setValidationErrors([]);
    setSubmitError(null);
  }, [activeIngredient, isModalOpen]);

  useEffect(() => {
    if (!deleteTarget) {
      return;
    }

    setDeleteError(null);
    setDeleteErrors([]);
  }, [deleteTarget]);

  const closeModal = () => {
    setIsCreateOpen(false);
    setSelectedIngredient(null);
  };

  const closeDeleteModal = () => {
    if (isDeleting) {
      return;
    }

    setDeleteTarget(null);
  };

  const appendIngredient = (ingredient: Ingredient) => {
    setIngredients((current) => {
      if (current.some((item) => item.id === ingredient.id)) {
        return current;
      }

      return [...current, ingredient].sort((left, right) =>
        left.name.localeCompare(right.name));
    });
  };

  const parseValidationErrors = (body: unknown) => {
    if (!body || typeof body !== "object") {
      return null;
    }

    const payload = body as IngredientErrorResponse;
    if (!Array.isArray(payload.errors)) {
      return null;
    }

    return payload;
  };

  const handleDelete = async () => {
    if (!deleteTarget || isDeleting) {
      return;
    }

    setIsDeleting(true);
    setDeleteError(null);
    setDeleteErrors([]);

    try {
      await apiClient.deleteIngredient(deleteTarget.id);
      setIngredients((current) =>
        current.filter((item) => item.id !== deleteTarget.id));
      setDeleteTarget(null);
    } catch (error) {
      if (error instanceof ApiError && [400, 404].includes(error.status)) {
        const payload = parseValidationErrors(error.body);
        if (payload) {
          setDeleteErrors(payload.errors);
          setDeleteError(payload.message ?? null);
          return;
        }
      }

      if (error instanceof Error) {
        setDeleteError(error.message);
      } else {
        setDeleteError("Failed to delete ingredient.");
      }
    } finally {
      setIsDeleting(false);
    }
  };

  const handleSave = async () => {
    if (isSaving) {
      return;
    }

    setIsSaving(true);
    setValidationErrors([]);
    setSubmitError(null);

    try {
      const payload = {
        name: formName.trim(),
        category: formCategory as Ingredient["category"],
        defaultUnit: formDefaultUnit as Ingredient["defaultUnit"],
      };

      if (isEditMode) {
        if (!selectedIngredient) {
          setSubmitError("Ingredient is required for updates.");
          return;
        }

        const updated = await apiClient.updateIngredient(
          selectedIngredient.id,
          payload,
        );
        setIngredients((current) =>
          current
            .map((item) => (item.id === updated.id ? updated : item))
            .sort((left, right) => left.name.localeCompare(right.name)));
        closeModal();
        return;
      }

      const created = await apiClient.createIngredient(payload);
      appendIngredient(created);
      closeModal();
    } catch (error) {
      if (error instanceof ApiError && [400, 404, 409].includes(error.status)) {
        const payload = parseValidationErrors(error.body);
        if (payload) {
          setValidationErrors(payload.errors);
          setSubmitError(payload.message ?? null);
          return;
        }
      }

      if (error instanceof Error) {
        setSubmitError(error.message);
      } else {
        setSubmitError("Failed to create ingredient.");
      }
    } finally {
      setIsSaving(false);
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
                <LeafIcon className="h-5 w-5" />
              </span>
              <div>
                <h1 className="text-2xl font-semibold text-[#1f2a22]">
                  Ingredients
                </h1>
                <p className="text-sm text-[#6c7a70]">
                  Manage your ingredient library ({usedIngredients} of{" "}
                  {totalIngredients} in use)
                </p>
              </div>
            </div>
            <button
              type="button"
              onClick={() => {
                setSelectedIngredient(null);
                setIsCreateOpen(true);
              }}
              className="inline-flex items-center gap-2 rounded-full bg-[#2f6b4f] px-4 py-2 text-sm font-semibold text-white shadow-[0_12px_24px_-18px_rgba(32,78,54,0.9)] transition hover:bg-[#2a5c46]"
            >
              <PlusIcon className="h-4 w-4" />
              Add Ingredient
            </button>
          </header>

          <div className="flex flex-wrap items-center justify-between gap-4">
            <div className="flex min-w-[240px] flex-1 items-center gap-3 rounded-2xl border border-[#e1e8dc] bg-white/80 px-4 py-3 shadow-[0_10px_30px_-26px_rgba(30,60,40,0.4)]">
              <SearchIcon className="h-4 w-4 text-[#7a887f]" />
              <input
                type="text"
                aria-label="Search ingredients"
                placeholder="Search ingredients..."
                value={searchQuery}
                onChange={(event) => setSearchQuery(event.target.value)}
                className="w-full bg-transparent text-sm text-[#2e3b33] placeholder:text-[#9aa69f] focus:outline-none"
              />
            </div>
            <div className="relative min-w-[180px]">
              <select
                value={selectedCategory}
                onChange={(event) => setSelectedCategory(event.target.value)}
                className="w-full appearance-none rounded-2xl border border-[#e1e8dc] bg-white/80 px-4 py-3 text-sm font-semibold text-[#3b4c42] shadow-[0_10px_30px_-26px_rgba(30,60,40,0.4)] focus:outline-none"
              >
                {categoryOptions.map((category) => (
                  <option key={category.value} value={category.value}>
                    {category.label}
                  </option>
                ))}
              </select>
              <ChevronDownIcon className="pointer-events-none absolute right-4 top-1/2 h-4 w-4 -translate-y-1/2 text-[#7a887f]" />
            </div>
          </div>

          <section className="overflow-hidden rounded-2xl border border-[#e1e8dc] bg-white/80 shadow-[0_16px_40px_-30px_rgba(35,60,42,0.35)]">
            <div className="overflow-x-auto">
              <table className="min-w-full text-sm">
                <thead className="bg-[#f6f8f3] text-left text-xs font-semibold uppercase tracking-[0.14em] text-[#7a887f]">
                  <tr>
                    <th className="px-5 py-4">Name</th>
                    <th className="px-5 py-4">Category</th>
                    <th className="px-5 py-4">Default Unit</th>
                    <th className="px-5 py-4">Used In</th>
                    <th className="px-5 py-4 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="text-[#2f3a33]">
                  {visibleIngredients.map((ingredient) => (
                    <tr
                      key={ingredient.id}
                      className="border-t border-[#edf1ea] transition hover:bg-[#f7f9f4]"
                    >
                      <td className="px-5 py-4 font-semibold text-[#1f2a22]">
                        {ingredient.name}
                      </td>
                      <td className="px-5 py-4">
                        <span className="inline-flex rounded-full bg-[#edf1ea] px-3 py-1 text-xs font-semibold text-[#4f5f55]">
                          {categoryLabels.get(ingredient.category)
                            ?? ingredient.category}
                        </span>
                      </td>
                      <td className="px-5 py-4 text-[#536157]">
                        {unitLabels.get(ingredient.defaultUnit)
                          ?? ingredient.defaultUnit}
                      </td>
                      <td className="px-5 py-4 text-[#6c7a70]">
                        {formatUsage(ingredient.usedIn)}
                      </td>
                      <td className="px-5 py-4">
                        <div className="flex items-center justify-end gap-2">
                          <button
                            type="button"
                            aria-label={`Edit ${ingredient.name}`}
                            onClick={() => {
                              setIsCreateOpen(false);
                              setSelectedIngredient(ingredient);
                            }}
                            className="grid h-9 w-9 place-items-center rounded-full border border-[#e3eadf] text-[#6e7c72] transition hover:bg-[#f4f7f1]"
                          >
                            <EditIcon className="h-4 w-4" />
                          </button>
                          <button
                            type="button"
                            aria-label={`Delete ${ingredient.name}`}
                            disabled={ingredient.usedIn > 0}
                            onClick={() => setDeleteTarget(ingredient)}
                            className="grid h-9 w-9 place-items-center rounded-full border border-[#f0dada] text-[#d76b6b] transition hover:bg-[#fbeeee] disabled:cursor-not-allowed disabled:border-[#f1e6e6] disabled:text-[#d6bcbc] disabled:hover:bg-transparent"
                          >
                            <TrashIcon className="h-4 w-4" />
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            {visibleIngredients.length === 0 ? (
              <div className="border-t border-[#edf1ea] px-6 py-6 text-sm text-[#6c7a70]">
                No ingredients match your search.
              </div>
            ) : null}
          </section>
        </main>
      </div>

      {deleteError && !deleteTarget ? (
        <div className="mt-6 rounded-2xl border border-[#f0dada] bg-[#fff6f6] px-4 py-3 text-sm text-[#b45151]">
          {deleteError}
        </div>
      ) : null}

      <Modal
        isOpen={isModalOpen}
        onClose={closeModal}
        title={isEditMode ? "Edit Ingredient" : "Add Ingredient"}
        description={
          isEditMode
            ? "Update the ingredient details below"
            : "Add the ingredient details below"
        }
        maxWidthClassName="max-w-xl"
        footer={
          <>
            <button
              type="button"
              onClick={closeModal}
              disabled={isSaving}
              className="inline-flex items-center justify-center rounded-xl border border-[#dfe6da] bg-white px-4 py-2 text-sm font-semibold text-[#3f4b43] transition hover:bg-[#f3f6ef]"
            >
              Cancel
            </button>
            <button
              type="button"
              onClick={() => {
                void handleSave();
              }}
              disabled={isSaving}
              className="inline-flex items-center justify-center rounded-xl bg-[#2f6b4f] px-4 py-2 text-sm font-semibold text-white shadow-[0_12px_22px_-18px_rgba(30,68,48,0.8)] transition hover:bg-[#2a5c46] disabled:cursor-not-allowed disabled:opacity-70"
            >
              {isSaving
                ? isEditMode
                  ? "Updating..."
                  : "Creating..."
                : isEditMode
                  ? "Update Ingredient"
                  : "Create Ingredient"}
            </button>
          </>
        }
      >
        {isSaving ? (
          <div className="mt-2 h-1 w-full overflow-hidden rounded-full bg-[#e1e7dd]">
            <div className="h-full w-1/2 animate-pulse rounded-full bg-[#2f6b4f]" />
          </div>
        ) : null}
        {submitError ? (
          <div className="mt-4 rounded-2xl border border-[#f0dada] bg-[#fff6f6] px-4 py-3 text-sm text-[#b45151]">
            {submitError}
          </div>
        ) : null}
        {validationErrors.length > 0 ? (
          <ul className="mt-4 grid gap-2 rounded-2xl border border-[#f0dada] bg-[#fff6f6] px-4 py-3 text-sm text-[#b45151]">
            {validationErrors.map((error, index) => (
              <li key={`${error.field}-${index}`}>
                {error.field ? `${error.field}: ` : ""}{error.message}
              </li>
            ))}
          </ul>
        ) : null}
        <div className="mt-6 grid gap-4">
          <label className="grid gap-2 text-sm font-semibold text-[#3f4b43]">
            Ingredient Name
            <input
              type="text"
              value={formName}
              onChange={(event) => setFormName(event.target.value)}
              placeholder="Ingredient name"
              className="rounded-xl border border-[#e1e7dd] bg-white px-3 py-2 text-sm text-[#2e3b33] focus:outline-none focus:ring-2 focus:ring-[#2f6b4f]/30"
            />
          </label>

          <div className="grid gap-4 sm:grid-cols-2">
            <label className="grid gap-2 text-sm font-semibold text-[#3f4b43]">
              Category
              <div className="relative">
                <select
                  value={formCategory}
                  onChange={(event) => setFormCategory(event.target.value as Ingredient["category"])}
                  className="w-full appearance-none rounded-xl border border-[#e1e7dd] bg-white px-3 py-2 text-sm text-[#2e3b33] focus:outline-none focus:ring-2 focus:ring-[#2f6b4f]/30"
                >
                  <option value="" disabled>
                    Select category
                  </option>
                  {modalCategoryOptions.map((category) => (
                    <option key={category.value} value={category.value}>
                      {category.label}
                    </option>
                  ))}
                </select>
                <ChevronDownIcon className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-[#7a887f]" />
              </div>
            </label>

            <label className="grid gap-2 text-sm font-semibold text-[#3f4b43]">
              Default Unit
              <div className="relative">
                <select
                  value={formDefaultUnit}
                  onChange={(event) => setFormDefaultUnit(event.target.value as Ingredient["defaultUnit"])}
                  className="w-full appearance-none rounded-xl border border-[#e1e7dd] bg-white px-3 py-2 text-sm text-[#2e3b33] focus:outline-none focus:ring-2 focus:ring-[#2f6b4f]/30"
                >
                  <option value="" disabled>
                    Select unit
                  </option>
                  {unitOptions.map((unit) => (
                    <option key={unit.value} value={unit.value}>
                      {unit.label}
                    </option>
                  ))}
                </select>
                <ChevronDownIcon className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-[#7a887f]" />
              </div>
            </label>
          </div>
        </div>
      </Modal>

      <Modal
        isOpen={deleteTarget !== null}
        onClose={closeDeleteModal}
        title="Delete Ingredient"
        description={
          deleteTarget
            ? `Are you sure you want to delete ${deleteTarget.name}?`
            : ""
        }
        maxWidthClassName="max-w-lg"
        footer={
          <>
            <button
              type="button"
              onClick={closeDeleteModal}
              disabled={isDeleting}
              className="inline-flex items-center justify-center rounded-xl border border-[#dfe6da] bg-white px-4 py-2 text-sm font-semibold text-[#3f4b43] transition hover:bg-[#f3f6ef]"
            >
              Cancel
            </button>
            <button
              type="button"
              onClick={() => {
                void handleDelete();
              }}
              disabled={isDeleting}
              className="inline-flex items-center justify-center rounded-xl bg-[#d76b6b] px-4 py-2 text-sm font-semibold text-white shadow-[0_12px_22px_-18px_rgba(180,80,80,0.8)] transition hover:bg-[#c85f5f] disabled:cursor-not-allowed disabled:opacity-70"
            >
              {isDeleting ? "Deleting..." : "Yes"}
            </button>
          </>
        }
      >
        {deleteError ? (
          <div className="mt-4 rounded-2xl border border-[#f0dada] bg-[#fff6f6] px-4 py-3 text-sm text-[#b45151]">
            {deleteError}
          </div>
        ) : null}
        {deleteErrors.length > 0 ? (
          <ul className="mt-4 grid gap-2 rounded-2xl border border-[#f0dada] bg-[#fff6f6] px-4 py-3 text-sm text-[#b45151]">
            {deleteErrors.map((error, index) => (
              <li key={`${error.field}-${index}`}>
                {error.field ? `${error.field}: ` : ""}{error.message}
              </li>
            ))}
          </ul>
        ) : null}
      </Modal>
    </div>
  );
}

function LeafIcon({ className }: { className?: string }) {
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
      <path d="M5 21c8.5 0 14-6.5 14-15V5h-1C9.5 5 5 9.5 5 17Z" />
      <path d="M7 17c3-4 6-6 11-7" />
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
