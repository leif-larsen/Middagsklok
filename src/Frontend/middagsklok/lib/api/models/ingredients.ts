import type {
  IngredientCategoryValue,
  IngredientUnitValue,
} from "../../../app/lib/models/ingredients";

export type DishIngredientInput = {
  name?: string | null;
  category?: string | null;
  amount: number;
  unit?: string | null;
};

export type DishOverviewIngredient = {
  id: string;
  label: string;
};

export type IngredientOverview = {
  id: string;
  name: string;
  category: IngredientCategoryValue;
  defaultUnit: IngredientUnitValue;
  usedIn: number;
};

export type IngredientsOverviewResponse = {
  ingredients: IngredientOverview[];
};

export type IngredientCategoryMetadata = {
  value: IngredientCategoryValue;
  label: string;
};

export type IngredientUnitMetadata = {
  value: IngredientUnitValue;
  label: string;
};

export type IngredientsMetadataResponse = {
  categories: IngredientCategoryMetadata[];
  units: IngredientUnitMetadata[];
};
