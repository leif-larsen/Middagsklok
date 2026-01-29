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
  IngredientCategoryMetadata,
  IngredientUnitMetadata,
  IngredientsMetadataResponse,
} from "../../lib/api/models/ingredients";

type IngredientsMetadataState = {
  categories: IngredientCategoryMetadata[];
  units: IngredientUnitMetadata[];
  isLoading: boolean;
  error: string | null;
};

const IngredientsMetadataContext =
  createContext<IngredientsMetadataState | null>(null);

const storageKey = "ingredients-metadata";

let cachedMetadata: IngredientsMetadataResponse | null = null;
let inFlightRequest: Promise<IngredientsMetadataResponse> | null = null;

const readStorage = (): IngredientsMetadataResponse | null => {
  if (typeof window === "undefined") {
    return null;
  }

  const raw = window.sessionStorage.getItem(storageKey);
  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw) as IngredientsMetadataResponse;
  } catch {
    return null;
  }
};

const writeStorage = (metadata: IngredientsMetadataResponse) => {
  if (typeof window === "undefined") {
    return;
  }

  window.sessionStorage.setItem(storageKey, JSON.stringify(metadata));
};

const loadMetadata = async (): Promise<IngredientsMetadataResponse> => {
  if (cachedMetadata) {
    return cachedMetadata;
  }

  if (!inFlightRequest) {
    inFlightRequest = apiClient
      .getIngredientsMetadata()
      .then((response) => {
        cachedMetadata = response;
        writeStorage(response);
        return response;
      })
      .finally(() => {
        inFlightRequest = null;
      });
  }

  return inFlightRequest;
};

export default function IngredientsMetadataProvider({
  children,
}: {
  children: ReactNode;
}) {
  const [state, setState] = useState<IngredientsMetadataState>(() => {
    const stored = readStorage();
    if (!stored) {
      return {
        categories: [],
        units: [],
        isLoading: true,
        error: null,
      };
    }

    cachedMetadata = stored;
    return {
      categories: stored.categories ?? [],
      units: stored.units ?? [],
      isLoading: false,
      error: null,
    };
  });

  useEffect(() => {
    if (cachedMetadata) {
      setState({
        categories: cachedMetadata.categories ?? [],
        units: cachedMetadata.units ?? [],
        isLoading: false,
        error: null,
      });
      return;
    }

    let isActive = true;

    const fetchMetadata = async () => {
      try {
        const response = await loadMetadata();
        if (!isActive) {
          return;
        }

        setState({
          categories: response.categories ?? [],
          units: response.units ?? [],
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
            error: "Failed to load ingredient metadata.",
          }));
        }
      }
    };

    void fetchMetadata();

    return () => {
      isActive = false;
    };
  }, []);

  const value = useMemo(() => state, [state]);

  return (
    <IngredientsMetadataContext.Provider value={value}>
      {children}
    </IngredientsMetadataContext.Provider>
  );
}

export const useIngredientsMetadata = (): IngredientsMetadataState => {
  const context = useContext(IngredientsMetadataContext);

  if (!context) {
    throw new Error("IngredientsMetadataProvider is missing.");
  }

  return context;
};
