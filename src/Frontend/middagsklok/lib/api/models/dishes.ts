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
  ingredients: DishOverviewIngredient[];
};

export type DishesOverviewResponse = {
  dishes: DishOverview[];
};
