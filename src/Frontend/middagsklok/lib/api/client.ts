import type {
  DishesImportRequest,
  DishesImportResponse,
  DishesOverviewResponse,
} from "./models/dishes";
import type {
  IngredientCreateRequest,
  IngredientCreateResponse,
  IngredientUpdateRequest,
  IngredientUpdateResponse,
  IngredientsMetadataResponse,
  IngredientsOverviewResponse,
} from "./models/ingredients";

export class ApiError extends Error {
  readonly status: number;
  readonly statusText: string;
  readonly body: unknown;

  constructor(message: string, status: number, statusText: string, body: unknown) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.statusText = statusText;
    this.body = body;
  }
}

const parseResponseBody = async (response: Response) => {
  if (response.status === 204) {
    return null;
  }

  const contentType = response.headers.get("content-type") ?? "";
  if (contentType.includes("application/json")) {
    return response.json();
  }

  return response.text();
};

const request = async <T>(
  path: string,
  init: RequestInit = {},
): Promise<T> => {
  const headers = new Headers(init.headers);
  if (init.body && !headers.has("content-type")) {
    headers.set("content-type", "application/json");
  }

  const response = await globalThis.fetch(path, {
    ...init,
    headers,
  });

  const body = await parseResponseBody(response);

  if (!response.ok) {
    throw new ApiError(
      `Request failed with ${response.status} ${response.statusText}`,
      response.status,
      response.statusText,
      body,
    );
  }

  return body as T;
};

export type ApiClient = {
  importDishes: (
    payload: DishesImportRequest,
    init?: RequestInit,
  ) => Promise<DishesImportResponse>;
  getDishes: (init?: RequestInit) => Promise<DishesOverviewResponse>;
  getIngredients: (init?: RequestInit) => Promise<IngredientsOverviewResponse>;
  createIngredient: (
    payload: IngredientCreateRequest,
    init?: RequestInit,
  ) => Promise<IngredientCreateResponse>;
  updateIngredient: (
    id: string,
    payload: IngredientUpdateRequest,
    init?: RequestInit,
  ) => Promise<IngredientUpdateResponse>;
  getIngredientsMetadata: (
    init?: RequestInit,
  ) => Promise<IngredientsMetadataResponse>;
};

export const apiClient: ApiClient = {
  importDishes: (payload, init) =>
    request<DishesImportResponse>("/api/dishes/import", {
      method: "POST",
      body: JSON.stringify(payload),
      ...init,
    }),
  getDishes: (init) =>
    request<DishesOverviewResponse>("/api/dishes", {
      method: "GET",
      ...init,
    }),
  getIngredients: (init) =>
    request<IngredientsOverviewResponse>("/api/ingredients", {
      method: "GET",
      ...init,
    }),
  createIngredient: (payload, init) =>
    request<IngredientCreateResponse>("/api/ingredients", {
      method: "POST",
      body: JSON.stringify(payload),
      ...init,
    }),
  updateIngredient: (id, payload, init) =>
    request<IngredientUpdateResponse>(`/api/ingredients/${id}`, {
      method: "PUT",
      body: JSON.stringify(payload),
      ...init,
    }),
  getIngredientsMetadata: (init) =>
    request<IngredientsMetadataResponse>("/api/ingredients/metadata", {
      method: "GET",
      ...init,
    }),
};
