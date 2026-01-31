import type {
  DishCreateRequest,
  DishCreateResponse,
  DishUpdateRequest,
  DishUpdateResponse,
  DishesImportRequest,
  DishesImportResponse,
  DishesLookupResponse,
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
import type {
  WeeklyPlanGenerateResponse,
  WeeklyPlanUpsertRequest,
  WeeklyPlanUpsertResponse,
  WeeklyPlansResponse,
  WeeklyPlanResponse,
} from "./models/weekly-plans";
import type {
  PlanningSettingsRequest,
  PlanningSettingsResponse,
} from "./models/settings";
import type { ShoppingListResponse } from "./models/shopping-list";

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
  createDish: (
    payload: DishCreateRequest,
    init?: RequestInit,
  ) => Promise<DishCreateResponse>;
  updateDish: (
    id: string,
    payload: DishUpdateRequest,
    init?: RequestInit,
  ) => Promise<DishUpdateResponse>;
  deleteDish: (id: string, init?: RequestInit) => Promise<void>;
  getDishes: (init?: RequestInit) => Promise<DishesOverviewResponse>;
  getDishesLookup: (init?: RequestInit) => Promise<DishesLookupResponse>;
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
  deleteIngredient: (id: string, init?: RequestInit) => Promise<void>;
  getIngredientsMetadata: (
    init?: RequestInit,
  ) => Promise<IngredientsMetadataResponse>;
  upsertWeeklyPlan: (
    startDate: string,
    payload: WeeklyPlanUpsertRequest,
    init?: RequestInit,
  ) => Promise<WeeklyPlanUpsertResponse>;
  generateWeeklyPlan: (
    startDate: string,
    init?: RequestInit,
  ) => Promise<WeeklyPlanGenerateResponse>;
  getWeeklyPlan: (
    startDate: string,
    init?: RequestInit,
  ) => Promise<WeeklyPlanResponse>;
  getWeeklyPlans: (init?: RequestInit) => Promise<WeeklyPlansResponse>;
  markWeeklyPlanEaten: (startDate: string, init?: RequestInit) => Promise<void>;
  getPlanningSettings: (init?: RequestInit) => Promise<PlanningSettingsResponse>;
  upsertPlanningSettings: (
    payload: PlanningSettingsRequest,
    init?: RequestInit,
  ) => Promise<PlanningSettingsResponse>;
  getShoppingList: (
    startDate: string,
    init?: RequestInit,
  ) => Promise<ShoppingListResponse>;
};

export const apiClient: ApiClient = {
  importDishes: (payload, init) =>
    request<DishesImportResponse>("/api/dishes/import", {
      method: "POST",
      body: JSON.stringify(payload),
      ...init,
    }),
  createDish: (payload, init) =>
    request<DishCreateResponse>("/api/dishes", {
      method: "POST",
      body: JSON.stringify(payload),
      ...init,
    }),
  updateDish: (id, payload, init) =>
    request<DishUpdateResponse>(`/api/dishes/${id}`, {
      method: "PUT",
      body: JSON.stringify(payload),
      ...init,
    }),
  deleteDish: (id, init) =>
    request<void>(`/api/dishes/${id}`, {
      method: "DELETE",
      ...init,
    }),
  getDishes: (init) =>
    request<DishesOverviewResponse>("/api/dishes", {
      method: "GET",
      ...init,
    }),
  getDishesLookup: (init) =>
    request<DishesLookupResponse>("/api/dishes/lookup", {
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
  deleteIngredient: (id, init) =>
    request<void>(`/api/ingredients/${id}`, {
      method: "DELETE",
      ...init,
    }),
  getIngredientsMetadata: (init) =>
    request<IngredientsMetadataResponse>("/api/ingredients/metadata", {
      method: "GET",
      ...init,
    }),
  upsertWeeklyPlan: (startDate, payload, init) =>
    request<WeeklyPlanUpsertResponse>(`/api/weekly-plans/${startDate}`, {
      method: "PUT",
      body: JSON.stringify(payload),
      ...init,
    }),
  generateWeeklyPlan: (startDate, init) =>
    request<WeeklyPlanGenerateResponse>(`/api/weekly-plans/generate/${startDate}`, {
      method: "POST",
      ...init,
    }),
  getWeeklyPlan: (startDate, init) =>
    request<WeeklyPlanResponse>(`/api/weekly-plans/${startDate}`, {
      method: "GET",
      ...init,
    }),
  getWeeklyPlans: (init) =>
    request<WeeklyPlansResponse>("/api/weekly-plans", {
      method: "GET",
      ...init,
    }),
  markWeeklyPlanEaten: (startDate, init) =>
    request<void>(`/api/weekly-plans/${startDate}/mark-eaten`, {
      method: "POST",
      ...init,
    }),
  getPlanningSettings: (init) =>
    request<PlanningSettingsResponse>("/api/planning-settings", {
      method: "GET",
      ...init,
    }),
  upsertPlanningSettings: (payload, init) =>
    request<PlanningSettingsResponse>("/api/planning-settings", {
      method: "PUT",
      body: JSON.stringify(payload),
      ...init,
    }),
  getShoppingList: (startDate, init) =>
    request<ShoppingListResponse>(`/api/shopping-list/${startDate}`, {
      method: "GET",
      ...init,
    }),
};
