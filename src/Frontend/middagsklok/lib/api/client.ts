type ClientOptions = {
  baseUrl?: string;
  fetcher?: typeof fetch;
};

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

const normalizeBaseUrl = (value?: string) => {
  if (!value) {
    return "";
  }

  return value.endsWith("/") ? value.slice(0, -1) : value;
};

const buildUrl = (baseUrl: string, path: string) => {
  const normalizedPath = path.startsWith("/") ? path : `/${path}`;

  if (!baseUrl) {
    return normalizedPath;
  }

  return `${baseUrl}${normalizedPath}`;
};

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

const getDefaultOptions = (): Required<ClientOptions> => ({
  baseUrl: normalizeBaseUrl(process.env.NEXT_PUBLIC_API_URL),
  fetcher: fetch,
});

const request = async <T>(
  options: Required<ClientOptions>,
  path: string,
  init: RequestInit = {},
): Promise<T> => {
  const headers = new Headers(init.headers);
  if (init.body && !headers.has("content-type")) {
    headers.set("content-type", "application/json");
  }

  const response = await options.fetcher(buildUrl(options.baseUrl, path), {
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
};

export const createApiClient = (options: ClientOptions = {}): ApiClient => {
  const resolvedOptions: Required<ClientOptions> = {
    ...getDefaultOptions(),
    ...options,
  };

  return {
    importDishes: (payload, init) =>
      request<DishesImportResponse>(resolvedOptions, "/dishes/import", {
        method: "POST",
        body: JSON.stringify(payload),
        ...init,
      }),
  };
};

export const apiClient = createApiClient();
