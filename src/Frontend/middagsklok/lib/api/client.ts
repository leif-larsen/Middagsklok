export type DishIngredientInput = {
  name?: string | null;
  category?: string | null;
  amount: number;
  unit?: string | null;
};

export type DishInput = {
  name?: string | null;
  activeMinutes: number;
  totalMinutes: number;
  ingredients?: DishIngredientInput[] | null;
};

export type DishesImportRequest = {
  dishes?: DishInput[] | null;
};

export type DishesImportFailure = {
  dishName?: string | null;
  reason: string;
  ingredientName?: string | null;
};

export type DishesImportResponse = {
  attempted: number;
  imported: number;
  skipped: number;
  failed: number;
  failures: DishesImportFailure[];
};

export type DishOverviewIngredient = {
  id: string;
  label: string;
};

export type DishOverview = {
  id: string;
  name: string;
  cuisine: string;
  prepMinutes: number;
  cookMinutes: number;
  serves: number;
  instructions?: string | null;
  ingredients: DishOverviewIngredient[];
};

export type DishesOverviewResponse = {
  dishes: DishOverview[];
};

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
};
