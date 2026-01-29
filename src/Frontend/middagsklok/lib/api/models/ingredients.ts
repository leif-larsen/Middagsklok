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
  category: string;
  defaultUnit: string;
  usedIn: number;
};

export type IngredientsOverviewResponse = {
  ingredients: IngredientOverview[];
};
