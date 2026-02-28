export type RecipeInstructionStep = {
  order: number;
  heading?: string | null;
  description: string;
};

export type RecipeIngredient = {
  ingredientId: string;
  name: string;
  quantity: number;
  unit: string;
  note?: string | null;
};

export type RecipeInstruction = {
  dishId: string;
  dishName: string;
  summary?: string | null;
  totalMinutes?: number | null;
  servings?: number | null;
  ingredients: RecipeIngredient[];
  steps: RecipeInstructionStep[];
};

export type RecipeInstructionsResponse = {
  recipes: RecipeInstruction[];
};

export type RecipeSuggestionRequest = {
  prompt: string;
  maxSuggestions?: number | null;
};

export type RecipeSuggestion = {
  id: string;
  title: string;
  summary: string;
  reason?: string | null;
  estimatedTotalMinutes?: number | null;
};

export type RecipeSuggestionsResponse = {
  suggestions: RecipeSuggestion[];
};

export type SaveFromSuggestionRequest = {
  title: string;
  summary: string;
  estimatedTotalMinutes?: number | null;
};

export type SaveFromSuggestionIngredient = {
  id: string;
  ingredientId: string;
  quantity: number;
  label: string;
};

export type SaveFromSuggestionResponse = {
  id: string;
  name: string;
  dishType: string;
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  servings: number;
  instructions?: string | null;
  isSeafood: boolean;
  isVegetarian: boolean;
  isVegan: boolean;
  vibeTags: string[];
  ingredients: SaveFromSuggestionIngredient[];
};
