import type { DishIngredientInput, DishOverviewIngredient } from "./ingredients";

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

export type DishOverview = {
  id: string;
  name: string;
  cuisine: string;
  prepMinutes: number;
  cookMinutes: number;
  serves: number;
  instructions?: string | null;
  isSeafood: boolean;
  isVegetarian: boolean;
  isVegan: boolean;
  ingredients: DishOverviewIngredient[];
};

export type DishesOverviewResponse = {
  dishes: DishOverview[];
};

export type DishLookup = {
  id: string;
  name: string;
  cuisine: string;
};

export type DishesLookupResponse = {
  dishes: DishLookup[];
};

export type DishCreateIngredientInput = {
  id?: string | null;
  name?: string | null;
  amount: number;
};

export type DishCreateRequest = {
  name?: string | null;
  cuisine?: string | null;
  prepMinutes: number;
  cookMinutes: number;
  serves: number;
  instructions?: string | null;
  isSeafood: boolean;
  isVegetarian: boolean;
  isVegan: boolean;
  ingredients?: DishCreateIngredientInput[] | null;
};

export type DishCreateValidationError = {
  field: string;
  message: string;
};

export type DishCreateErrorResponse = {
  message: string;
  errors: DishCreateValidationError[];
};

export type DishCreateResponse = DishOverview;

export type DishUpdateRequest = DishCreateRequest;

export type DishUpdateResponse = DishOverview;

export type DishUpdateValidationError = DishCreateValidationError;

export type DishUpdateErrorResponse = DishCreateErrorResponse;
