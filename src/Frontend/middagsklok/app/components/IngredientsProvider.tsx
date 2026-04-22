"use client";

import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { ApiError, apiClient } from "../../lib/api/client";
import type {
  IngredientOverview,
  IngredientsOverviewResponse,
} from "../../lib/api/models/ingredients";

type IngredientsState = {
  ingredients: IngredientOverview[];
  isLoading: boolean;
  error: string | null;
  appendIngredient: (ingredient: IngredientOverview) => void;
  replaceIngredient: (ingredient: IngredientOverview) => void;
  removeIngredient: (id: string) => void;
};

const IngredientsContext = createContext<IngredientsState | null>(null);

const storageKey = "ingredients-overview";

let cachedIngredients: IngredientsOverviewResponse | null = null;
let inFlightRequest: Promise<IngredientsOverviewResponse> | null = null;

const readStorage = (): IngredientsOverviewResponse | null => {
  if (typeof window === "undefined") {
    return null;
  }

  const raw = window.sessionStorage.getItem(storageKey);
  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw) as IngredientsOverviewResponse;
  } catch {
    return null;
  }
};

const writeStorage = (response: IngredientsOverviewResponse) => {
  if (typeof window === "undefined") {
    return;
  }

  window.sessionStorage.setItem(storageKey, JSON.stringify(response));
};

const loadIngredients = async (): Promise<IngredientsOverviewResponse> => {
  if (cachedIngredients) {
    return cachedIngredients;
  }

  if (!inFlightRequest) {
    inFlightRequest = apiClient
      .getIngredients()
      .then((response) => {
        cachedIngredients = response;
        writeStorage(response);
        return response;
      })
      .finally(() => {
        inFlightRequest = null;
      });
  }

  return inFlightRequest;
};

type IngredientsData = {
  ingredients: IngredientOverview[];
  isLoading: boolean;
  error: string | null;
};

const syncCache = (ingredients: IngredientOverview[]) => {
  const updated: IngredientsOverviewResponse = { ingredients };
  cachedIngredients = updated;
  writeStorage(updated);
};

export default function IngredientsProvider({
  children,
}: {
  children: ReactNode;
}) {
  const [data, setData] = useState<IngredientsData>(() => {
    const stored = readStorage();
    if (!stored) {
      return {
        ingredients: [],
        isLoading: true,
        error: null,
      };
    }

    cachedIngredients = stored;
    return {
      ingredients: stored.ingredients ?? [],
      isLoading: false,
      error: null,
    };
  });

  useEffect(() => {
    if (cachedIngredients) {
      setData({
        ingredients: cachedIngredients.ingredients ?? [],
        isLoading: false,
        error: null,
      });
      return;
    }

    let isActive = true;

    const fetchIngredients = async () => {
      try {
        const response = await loadIngredients();
        if (!isActive) {
          return;
        }

        setData({
          ingredients: response.ingredients ?? [],
          isLoading: false,
          error: null,
        });
      } catch (error) {
        if (!isActive) {
          return;
        }

        if (error instanceof ApiError) {
          setData((current) => ({
            ...current,
            isLoading: false,
            error: error.message,
          }));
        } else if (error instanceof Error) {
          setData((current) => ({
            ...current,
            isLoading: false,
            error: error.message,
          }));
        } else {
          setData((current) => ({
            ...current,
            isLoading: false,
            error: "Failed to load ingredients.",
          }));
        }
      }
    };

    void fetchIngredients();

    return () => {
      isActive = false;
    };
  }, []);

  const appendIngredient = useMemo(
    () => (ingredient: IngredientOverview) => {
      setData((current) => {
        if (current.ingredients.some((item) => item.id === ingredient.id)) {
          return current;
        }
        const updated = [...current.ingredients, ingredient].sort((a, b) =>
          a.name.localeCompare(b.name),
        );
        syncCache(updated);
        return { ...current, ingredients: updated };
      });
    },
    [],
  );

  const replaceIngredient = useMemo(
    () => (ingredient: IngredientOverview) => {
      setData((current) => {
        const updated = current.ingredients
          .map((item) => (item.id === ingredient.id ? ingredient : item))
          .sort((a, b) => a.name.localeCompare(b.name));
        syncCache(updated);
        return { ...current, ingredients: updated };
      });
    },
    [],
  );

  const removeIngredient = useMemo(
    () => (id: string) => {
      setData((current) => {
        const updated = current.ingredients.filter((item) => item.id !== id);
        syncCache(updated);
        return { ...current, ingredients: updated };
      });
    },
    [],
  );

  const value = useMemo(
    () => ({ ...data, appendIngredient, replaceIngredient, removeIngredient }),
    [data, appendIngredient, replaceIngredient, removeIngredient],
  );

  return (
    <IngredientsContext.Provider value={value}>
      {children}
    </IngredientsContext.Provider>
  );
}

export const useIngredientsCatalog = (): IngredientsState => {
  const context = useContext(IngredientsContext);

  if (!context) {
    throw new Error("IngredientsProvider is missing.");
  }

  return context;
};
