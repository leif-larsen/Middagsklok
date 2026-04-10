"use client";

import Sidebar from "../components/Sidebar";
import Modal from "../components/Modal";
import { apiClient, ApiError } from "../../lib/api/client";
import { useEffect, useMemo, useState } from "react";
import { useDishesMetadata } from "../components/DishesMetadataProvider";
import { useIngredientsCatalog } from "../components/IngredientsProvider";
import type {
  DishCreateErrorResponse,
  DishCreateValidationError,
  DishesImportRequest,
  DishUpdateErrorResponse,
  DishUpdateValidationError,
} from "../../lib/api/models/dishes";

type Ingredient = {
  id: string;
  ingredientId: string;
  amount: number;
  label: string;
};

type DraftIngredient = {
  id: string;
  ingredientId: string;
  label: string;
  amount?: string | null;
};

type Dish = {
  id: string;
  name: string;
  dishType: string;
  prepMinutes: number;
  cookMinutes: number;
  serves: number;
  instructions?: string | null;
  isSeafood: boolean;
  isVegetarian: boolean;
  isVegan: boolean;
  ingredients: Ingredient[];
  lastEatenOn?: string | null;
};

const emptyDish: Dish = {
  id: "new-dish",
  name: "",
  dishType: "",
  prepMinutes: 0,
  cookMinutes: 0,
  serves: 0,
  instructions: "",
  isSeafood: false,
  isVegetarian: false,
  isVegan: false,
  ingredients: [],
};

const formatMinutes = (value: number) => `${value}m`;

export default function DishesPage() {
  const [searchQuery, setSearchQuery] = useState("");
  const [sortBy, setSortBy] = useState<"name" | "lastEatenDesc" | "lastEatenAsc">("name");
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
  const [formName, setFormName] = useState("");
  const [formDishType, setFormDishType] = useState("");
  const [formPrepMinutes, setFormPrepMinutes] = useState("");
  const [formCookMinutes, setFormCookMinutes] = useState("");
  const [formServes, setFormServes] = useState("");
  const [formInstructions, setFormInstructions] = useState("");
  const [formIsSeafood, setFormIsSeafood] = useState(false);
  const [formIsVegetarian, setFormIsVegetarian] = useState(false);
  const [formIsVegan, setFormIsVegan] = useState(false);
  const [validationErrors, setValidationErrors] = useState<
    DishCreateValidationError[] | DishUpdateValidationError[]
  >([]);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<Dish | null>(null);
  const [deleteError, setDeleteError] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [draftIngredients, setDraftIngredients] = useState<DraftIngredient[]>([]);
  const [selectedIngredientId, setSelectedIngredientId] = useState("");
  const [ingredientAmount, setIngredientAmount] = useState("");
  const {
    dishTypes: dishTypeMetadata,
    isLoading: dishTypesLoading,
    error: dishTypesError,
  } = useDishesMetadata();
  const {
    ingredients: availableIngredients,
    isLoading: ingredientsLoading,
    error: ingredientsError,
  } = useIngredientsCatalog();

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

  const ingredientOptions = useMemo(
    () =>
      [...availableIngredients].sort((left, right) =>
        left.name.localeCompare(right.name)),
    [availableIngredients],
  );

  const ingredientSelectMessage = ingredientsLoading
    ? "Loading ingredients..."
    : ingredientsError
      ? "Unable to load ingredients"
      : ingredientOptions.length === 0
        ? "No ingredients available"
        : "Select an ingredient";

  const isIngredientSelectDisabled = ingredientsLoading
    || !!ingredientsError
    || ingredientOptions.length === 0;

  const dishTypeOptions = useMemo(
    () =>
      dishTypeMetadata
        .filter((dishType) => dishType.isSelectable)
        .sort((left, right) => left.order - right.order || left.label.localeCompare(right.label)),
    [dishTypeMetadata],
  );

  const dishTypeLabelMap = useMemo(
    () =>
      new Map(
        dishTypeMetadata.map((dishType) => [dishType.value, dishType.label]),
      ),
    [dishTypeMetadata],
  );

  const defaultDishType = useMemo(() => {
    const other = dishTypeOptions.find((dishType) => dishType.value === "Other");
    if (other) {
      return other.value;
    }

    const first = dishTypeOptions[0];
    return first ? first.value : "Other";
  }, [dishTypeOptions]);

  const formDishTypeOptions = useMemo(() => {
    if (!formDishType || dishTypeOptions.some((dishType) => dishType.value === formDishType)) {
      return dishTypeOptions;
    }

    return [
      {
        value: formDishType,
        label: formDishType,
        order: Number.MIN_SAFE_INTEGER,
        isSelectable: true,
      },
      ...dishTypeOptions,
    ];
  }, [dishTypeOptions, formDishType]);

  const dishTypeSelectMessage = dishTypesLoading
    ? "Loading dish types..."
    : formDishTypeOptions.length === 0
      ? "No dish types available"
      : "Select dish type";

  const visibleDishes = useMemo(() => {
    const query = searchQuery.trim().toLowerCase();
    const filtered = query
      ? dishes.filter((dish) => dish.name.toLowerCase().includes(query))
      : dishes;

    if (sortBy === "lastEatenDesc" || sortBy === "lastEatenAsc") {
      const dir = sortBy === "lastEatenDesc" ? -1 : 1;
      return [...filtered].sort((a, b) => {
        if (!a.lastEatenOn && !b.lastEatenOn) return a.name.localeCompare(b.name);
        if (!a.lastEatenOn) return 1;
        if (!b.lastEatenOn) return -1;
        return dir * b.lastEatenOn.localeCompare(a.lastEatenOn);
      });
    }

    return filtered;
  }, [dishes, searchQuery, sortBy]);

  const isModalOpen = isCreateOpen || selectedDish !== null;
  const isEditMode = selectedDish !== null;

  useEffect(() => {
    if (!isModalOpen) {
      return;
    }

    setFormName(activeDish.name ?? "");
    const normalizedDishType =
      isEditMode
        ? (!activeDish.dishType || activeDish.dishType === "None"
            ? defaultDishType
            : activeDish.dishType)
        : defaultDishType;
    setFormDishType(normalizedDishType);
    setFormPrepMinutes(
      isEditMode ? String(activeDish.prepMinutes ?? 0) : "",
    );
    setFormCookMinutes(
      isEditMode ? String(activeDish.cookMinutes ?? 0) : "",
    );
    setFormServes(
      isEditMode ? String(activeDish.serves ?? 0) : "",
    );
    setFormInstructions(activeDish.instructions ?? "");
    setFormIsSeafood(activeDish.isSeafood ?? false);
    setFormIsVegetarian(activeDish.isVegetarian ?? false);
    setFormIsVegan(activeDish.isVegan ?? false);
    setValidationErrors([]);
    setSubmitError(null);
    setIsSaving(false);
    setDraftIngredients(
      activeDish.ingredients.map((ingredient) => ({
        id: ingredient.ingredientId,
        ingredientId: ingredient.ingredientId,
        label: ingredient.label,
        amount: String(ingredient.amount),
      })),
    );
    setSelectedIngredientId("");
    setIngredientAmount("");
  }, [activeDish, defaultDishType, isEditMode, isModalOpen]);

  useEffect(() => {
    if (!deleteTarget) {
      return;
    }

    setDeleteError(null);
  }, [deleteTarget]);

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

  const closeDeleteModal = () => {
    if (isDeleting) {
      return;
    }

    setDeleteTarget(null);
    setDeleteError(null);
  };

  const appendDish = (dish: Dish) => {
    setDishes((current) => {
      if (current.some((item) => item.id === dish.id)) {
        return current;
      }

      return [...current, dish].sort((left, right) =>
        left.name.localeCompare(right.name));
    });
  };

  const parseValidationErrors = (body: unknown) => {
    if (!body || typeof body !== "object") {
      return null;
    }

    const payload = body as DishCreateErrorResponse | DishUpdateErrorResponse;
    if (!Array.isArray(payload.errors)) {
      return null;
    }

    return payload;
  };

  const parseNumber = (value: string) => {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : 0;
  };

  const parseImportPayload = async (file: File) => {
    const raw = await file.text();
    const parsed = JSON.parse(raw) as unknown;

    if (Array.isArray(parsed)) {
      return { dishes: parsed } as DishesImportRequest;
    }

    if (parsed && typeof parsed === "object" && "dishes" in parsed) {
      return parsed as DishesImportRequest;
    }

    throw new Error("Invalid JSON format. Expected an array or an object with a dishes property.");
  };

  const handleSave = async () => {
    if (isSaving) {
      return;
    }

    setIsSaving(true);
    setValidationErrors([]);
    setSubmitError(null);

    try {
      const name = formName.trim();
      const dishType = formDishType.trim();
      const instructions = formInstructions.trim();
      const payload = {
        name,
        dishType: dishType ? dishType : defaultDishType,
        prepMinutes: parseNumber(formPrepMinutes),
        cookMinutes: parseNumber(formCookMinutes),
        serves: parseNumber(formServes),
        instructions: instructions ? instructions : null,
        isSeafood: formIsSeafood,
        isVegetarian: formIsVegetarian,
        isVegan: formIsVegan,
        ingredients: draftIngredients.map((ingredient) => ({
          id: ingredient.ingredientId,
          name: ingredient.label,
          amount: parseNumber(ingredient.amount ?? ""),
        })),
      };

      if (isEditMode) {
        if (!selectedDish) {
          setSubmitError("Dish is required for updates.");
          return;
        }

        const updated = await apiClient.updateDish(selectedDish.id, payload);
        setDishes((current) =>
          current
            .map((dish) => (dish.id === updated.id ? updated : dish))
            .sort((left, right) => left.name.localeCompare(right.name)));
        closeModal();
        return;
      }

      const created = await apiClient.createDish(payload);
      appendDish(created);
      closeModal();
    } catch (error) {
      if (error instanceof ApiError && [400, 409].includes(error.status)) {
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
        setSubmitError("Failed to create dish.");
      }
    } finally {
      setIsSaving(false);
    }
  };

  const handleDelete = async () => {
    if (!deleteTarget || isDeleting) {
      return;
    }

    setIsDeleting(true);
    setDeleteError(null);

    try {
      await apiClient.deleteDish(deleteTarget.id);
      setDishes((current) =>
        current.filter((dish) => dish.id !== deleteTarget.id));
      setDeleteTarget(null);
    } catch (error) {
      if (error instanceof Error) {
        setDeleteError(error.message);
      } else {
        setDeleteError("Failed to delete dish.");
      }
    } finally {
      setIsDeleting(false);
    }
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

  const handleAddIngredient = () => {
    if (!selectedIngredientId) {
      return;
    }

    const selected = ingredientOptions.find(
      (ingredient) => ingredient.id === selectedIngredientId,
    );

    if (!selected) {
      return;
    }

    const normalizedAmount = ingredientAmount.trim();

    setDraftIngredients((current) => {
      if (current.some((ingredient) => ingredient.id === selected.id)) {
        return current;
      }

      return [
        ...current,
        {
          id: selected.id,
          ingredientId: selected.id,
          label: selected.name,
          amount: normalizedAmount ? normalizedAmount : null,
        },
      ];
    });
    setSelectedIngredientId("");
    setIngredientAmount("");
  };

  const handleRemoveIngredient = (ingredientId: string) => {
    setDraftIngredients((current) =>
      current.filter((ingredient) => ingredient.id !== ingredientId));
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

          <div className="flex gap-3">
            <div className="flex flex-1 items-center gap-3 rounded-2xl border border-[#e1e8dc] bg-white/80 px-4 py-3 shadow-[0_10px_30px_-26px_rgba(30,60,40,0.4)]">
              <SearchIcon className="h-4 w-4 text-[#7a887f]" />
              <input
                type="text"
                aria-label="Search dishes"
                placeholder="Search dishes by name..."
                value={searchQuery}
                onChange={(event) => setSearchQuery(event.target.value)}
                className="w-full bg-transparent text-sm text-[#2e3b33] placeholder:text-[#9aa69f] focus:outline-none"
              />
            </div>
            <select
              aria-label="Sort dishes by"
              value={sortBy}
              onChange={(event) => setSortBy(event.target.value as "name" | "lastEatenDesc" | "lastEatenAsc")}
              className="rounded-2xl border border-[#e1e8dc] bg-white/80 px-4 py-3 text-sm font-semibold text-[#3d4c43] shadow-[0_10px_30px_-26px_rgba(30,60,40,0.4)] focus:outline-none"
            >
              <option value="name">Sort: Name</option>
              <option value="lastEatenDesc">Sort: Last eaten (newest first)</option>
              <option value="lastEatenAsc">Sort: Last eaten (oldest first)</option>
            </select>
          </div>

          <section className="grid gap-5 lg:grid-cols-2 xl:grid-cols-3">
            {visibleDishes.map((dish) => {
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
                        {dishTypeLabelMap.get(dish.dishType) ?? dish.dishType}
                      </span>
                      {dish.isSeafood ? (
                        <span className="mt-2 ml-2 inline-flex rounded-full bg-[#e6f5ff] px-3 py-1 text-xs font-semibold text-[#1d5b7a]">
                          Seafood
                        </span>
                      ) : null}
                      {dish.isVegetarian ? (
                        <span className="mt-2 ml-2 inline-flex rounded-full bg-[#eaf6e4] px-3 py-1 text-xs font-semibold text-[#2d6a3e]">
                          Vegetarian
                        </span>
                      ) : null}
                      {dish.isVegan ? (
                        <span className="mt-2 ml-2 inline-flex rounded-full bg-[#e2f5e9] px-3 py-1 text-xs font-semibold text-[#21573a]">
                          Vegan
                        </span>
                      ) : null}
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
                        onClick={() => setDeleteTarget(dish)}
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
                    <span className="flex items-center gap-2">
                      <CalendarIcon className="h-4 w-4 text-[#2f6b4f]" />
                      {dish.lastEatenOn
                        ? `Last eaten: ${dish.lastEatenOn}`
                        : "Never eaten"}
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
              disabled={isSaving}
              className="inline-flex items-center justify-center rounded-xl border border-[#dfe6da] bg-white px-4 py-2 text-sm font-semibold text-[#3f4b43] transition hover:bg-[#f3f6ef] disabled:cursor-not-allowed disabled:opacity-70"
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
                  ? "Update Dish"
                  : "Create Dish"}
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
          <div className="grid gap-4 sm:grid-cols-2">
            <label className="grid gap-2 text-sm font-semibold text-[#3f4b43]">
              Dish Name
              <input
                type="text"
                value={formName}
                onChange={(event) => setFormName(event.target.value)}
                placeholder="Dish name"
                className="rounded-xl border border-[#e1e7dd] bg-white px-3 py-2 text-sm text-[#2e3b33] focus:outline-none focus:ring-2 focus:ring-[#2f6b4f]/30"
              />
            </label>
            <label className="grid gap-2 text-sm font-semibold text-[#3f4b43]">
              Dish type
              <select
                value={formDishType}
                onChange={(event) => setFormDishType(event.target.value)}
                className="rounded-xl border border-[#e1e7dd] bg-white px-3 py-2 text-sm text-[#2e3b33] focus:outline-none focus:ring-2 focus:ring-[#2f6b4f]/30 disabled:cursor-not-allowed disabled:bg-[#f6f8f4] disabled:text-[#9aa69f]"
                disabled={dishTypesLoading || formDishTypeOptions.length === 0}
              >
                {formDishTypeOptions.length === 0 ? (
                  <option value="">
                    {dishTypeSelectMessage}
                  </option>
                ) : null}
                {formDishTypeOptions.map((dishType) => (
                  <option key={dishType.value} value={dishType.value}>
                    {dishType.label}
                  </option>
                ))}
              </select>
              {dishTypesError ? (
                <span className="text-xs font-normal text-[#b14a4a]">
                  {dishTypesError}
                </span>
              ) : null}
            </label>
          </div>
          <div className="grid gap-3 sm:grid-cols-3">
            <label className="inline-flex items-center gap-3 rounded-xl border border-[#e1e7dd] bg-white px-3 py-2 text-sm font-semibold text-[#3f4b43]">
              <input
                type="checkbox"
                checked={formIsSeafood}
                onChange={(event) => setFormIsSeafood(event.target.checked)}
                className="h-4 w-4 rounded border-[#b9c8bd] text-[#2f6b4f] focus:ring-[#2f6b4f]/30"
              />
              Seafood dish
            </label>
            <label className="inline-flex items-center gap-3 rounded-xl border border-[#e1e7dd] bg-white px-3 py-2 text-sm font-semibold text-[#3f4b43]">
              <input
                type="checkbox"
                checked={formIsVegetarian}
                onChange={(event) => setFormIsVegetarian(event.target.checked)}
                className="h-4 w-4 rounded border-[#b9c8bd] text-[#2f6b4f] focus:ring-[#2f6b4f]/30"
              />
              Vegetarian dish
            </label>
            <label className="inline-flex items-center gap-3 rounded-xl border border-[#e1e7dd] bg-white px-3 py-2 text-sm font-semibold text-[#3f4b43]">
              <input
                type="checkbox"
                checked={formIsVegan}
                onChange={(event) => setFormIsVegan(event.target.checked)}
                className="h-4 w-4 rounded border-[#b9c8bd] text-[#2f6b4f] focus:ring-[#2f6b4f]/30"
              />
              Vegan dish
            </label>
          </div>

          <div className="grid gap-8 sm:grid-cols-2 lg:grid-cols-3">
            <label className="grid gap-2 text-sm font-semibold text-[#3f4b43]">
              Prep Time (min)
              <input
                type="number"
                value={formPrepMinutes}
                onChange={(event) => setFormPrepMinutes(event.target.value)}
                placeholder="0"
                className="rounded-xl border border-[#e1e7dd] bg-white px-3 py-2 text-sm text-[#2e3b33] focus:outline-none focus:ring-2 focus:ring-[#2f6b4f]/30"
              />
            </label>
            <label className="grid gap-2 text-sm font-semibold text-[#3f4b43]">
              Cook Time (min)
              <input
                type="number"
                value={formCookMinutes}
                onChange={(event) => setFormCookMinutes(event.target.value)}
                placeholder="0"
                className="rounded-xl border border-[#e1e7dd] bg-white px-3 py-2 text-sm text-[#2e3b33] focus:outline-none focus:ring-2 focus:ring-[#2f6b4f]/30"
              />
            </label>
            <label className="grid gap-2 text-sm font-semibold text-[#3f4b43]">
              Servings
              <input
                type="number"
                value={formServes}
                onChange={(event) => setFormServes(event.target.value)}
                placeholder="0"
                className="rounded-xl border border-[#e1e7dd] bg-white px-3 py-2 text-sm text-[#2e3b33] focus:outline-none focus:ring-2 focus:ring-[#2f6b4f]/30"
              />
            </label>
          </div>

          <label className="grid gap-2 text-sm font-semibold text-[#3f4b43]">
            Instructions (optional)
            <textarea
              value={formInstructions}
              onChange={(event) => setFormInstructions(event.target.value)}
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
              <div className="relative">
                <select
                  aria-label="Select ingredient"
                  value={selectedIngredientId}
                  onChange={(event) =>
                    setSelectedIngredientId(event.target.value)}
                  disabled={isIngredientSelectDisabled}
                  className="w-full appearance-none rounded-xl border border-[#e1e7dd] bg-white px-3 py-2 text-sm text-[#2e3b33] focus:outline-none focus:ring-2 focus:ring-[#2f6b4f]/30 disabled:cursor-not-allowed disabled:bg-[#f6f8f4] disabled:text-[#9aa69f]"
                >
                  <option value="" disabled>
                    {ingredientSelectMessage}
                  </option>
                  {ingredientOptions.map((ingredient) => (
                    <option key={ingredient.id} value={ingredient.id}>
                      {ingredient.name}
                    </option>
                  ))}
                </select>
                <ChevronDownIcon className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-[#7a887f]" />
              </div>
              <input
                type="text"
                placeholder="Amount"
                value={ingredientAmount}
                onChange={(event) => setIngredientAmount(event.target.value)}
                className="rounded-xl border border-[#e1e7dd] bg-white px-3 py-2 text-sm text-[#2e3b33] focus:outline-none focus:ring-2 focus:ring-[#2f6b4f]/30"
              />
              <button
                type="button"
                onClick={handleAddIngredient}
                disabled={isIngredientSelectDisabled || !selectedIngredientId}
                className="inline-flex items-center justify-center rounded-xl border border-[#dfe6da] bg-white px-4 py-2 text-sm font-semibold text-[#3f4b43] transition hover:bg-[#f3f6ef] disabled:cursor-not-allowed disabled:opacity-60"
              >
                Add
              </button>
            </div>
            {ingredientsError ? (
              <p className="mt-2 text-xs text-[#b14a4a]">
                {ingredientsError}
              </p>
            ) : null}

            <ul className="mt-4 space-y-3 text-sm text-[#3d4c43]">
              {draftIngredients.length > 0 ? (
                draftIngredients.map((ingredient) => (
                  <li
                    key={ingredient.id}
                    className="flex items-center justify-between gap-3"
                  >
                    <span className="flex items-center gap-2">
                      <span className="h-2 w-2 rounded-full bg-[#9bb09f]" />
                      {ingredient.label}
                      {ingredient.amount ? (
                        <span className="text-xs text-[#7a887f]">
                          ({ingredient.amount})
                        </span>
                      ) : null}
                    </span>
                    <button
                      type="button"
                      aria-label={`Remove ${ingredient.label}`}
                      onClick={() => handleRemoveIngredient(ingredient.id)}
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
        isOpen={deleteTarget !== null}
        onClose={closeDeleteModal}
        title="Delete Dish"
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
              className="inline-flex items-center justify-center rounded-xl border border-[#dfe6da] bg-white px-4 py-2 text-sm font-semibold text-[#3f4b43] transition hover:bg-[#f3f6ef] disabled:cursor-not-allowed disabled:opacity-70"
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

function CalendarIcon({ className }: { className?: string }) {
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
      <rect x="3" y="4" width="18" height="18" rx="2" ry="2" />
      <line x1="16" y1="2" x2="16" y2="6" />
      <line x1="8" y1="2" x2="8" y2="6" />
      <line x1="3" y1="10" x2="21" y2="10" />
    </svg>
  );
}
