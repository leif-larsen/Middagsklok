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

export default function IngredientsProvider({
  children,
}: {
  children: ReactNode;
}) {
  const [state, setState] = useState<IngredientsState>(() => {
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
      setState({
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

        setState({
          ingredients: response.ingredients ?? [],
          isLoading: false,
          error: null,
        });
      } catch (error) {
        if (!isActive) {
          return;
        }

        if (error instanceof ApiError) {
          setState((current) => ({
            ...current,
            isLoading: false,
            error: error.message,
          }));
        } else if (error instanceof Error) {
          setState((current) => ({
            ...current,
            isLoading: false,
            error: error.message,
          }));
        } else {
          setState((current) => ({
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

  const value = useMemo(() => state, [state]);

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
