export type RecipeInstructionStep = {
  order: number;
  heading?: string | null;
  description: string;
};

export type RecipeInstruction = {
  dishId: string;
  dishName: string;
  summary?: string | null;
  totalMinutes?: number | null;
  servings?: number | null;
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
