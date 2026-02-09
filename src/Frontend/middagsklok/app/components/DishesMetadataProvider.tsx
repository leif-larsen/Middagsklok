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
  DishCuisineMetadata,
  DishesMetadataResponse,
} from "../../lib/api/models/dishes";

type DishesMetadataState = {
  cuisines: DishCuisineMetadata[];
  isLoading: boolean;
  error: string | null;
};

const DishesMetadataContext = createContext<DishesMetadataState | null>(null);

const storageKey = "dishes-metadata";

let cachedMetadata: DishesMetadataResponse | null = null;
let inFlightRequest: Promise<DishesMetadataResponse> | null = null;

const readStorage = (): DishesMetadataResponse | null => {
  if (typeof window === "undefined") {
    return null;
  }

  const raw = window.sessionStorage.getItem(storageKey);
  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw) as DishesMetadataResponse;
  } catch {
    return null;
  }
};

const writeStorage = (metadata: DishesMetadataResponse) => {
  if (typeof window === "undefined") {
    return;
  }

  window.sessionStorage.setItem(storageKey, JSON.stringify(metadata));
};

const loadMetadata = async (): Promise<DishesMetadataResponse> => {
  if (cachedMetadata) {
    return cachedMetadata;
  }

  if (!inFlightRequest) {
    inFlightRequest = apiClient
      .getDishesMetadata()
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

export default function DishesMetadataProvider({
  children,
}: {
  children: ReactNode;
}) {
  const [state, setState] = useState<DishesMetadataState>(() => {
    const stored = readStorage();
    if (!stored) {
      return {
        cuisines: [],
        isLoading: true,
        error: null,
      };
    }

    cachedMetadata = stored;
    return {
      cuisines: stored.cuisines ?? [],
      isLoading: false,
      error: null,
    };
  });

  useEffect(() => {
    if (cachedMetadata) {
      setState({
        cuisines: cachedMetadata.cuisines ?? [],
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
          cuisines: response.cuisines ?? [],
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
            error: "Failed to load dish metadata.",
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
    <DishesMetadataContext.Provider value={value}>
      {children}
    </DishesMetadataContext.Provider>
  );
}

export const useDishesMetadata = (): DishesMetadataState =>
{
  const context = useContext(DishesMetadataContext);

  if (!context) {
    throw new Error("DishesMetadataProvider is missing.");
  }

  return context;
};
