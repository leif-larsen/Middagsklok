export type ShoppingListItem = {
  ingredientId: string;
  name: string;
  amount: number;
  unit: string;
};

export type ShoppingListCategory = {
  category: string;
  items: ShoppingListItem[];
};

export type ShoppingListResponse = {
  startDate: string;
  categories: ShoppingListCategory[];
};
