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
