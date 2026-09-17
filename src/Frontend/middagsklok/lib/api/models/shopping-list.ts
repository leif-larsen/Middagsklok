export type OdaProduct = {
  productId: number;
  productName: string;
  packQuantity: number;
  packUnit: string;
  suggestedPackCount: number | null;
  confirmed: boolean;
};

export type OdaStatus = "Unmapped" | "Mapped" | "NotAvailable";

export type ShoppingListItem = {
  ingredientId: string;
  name: string;
  amount: number;
  unit: string;
  dishes: string[];
  isPantryStaple: boolean;
  odaStatus: OdaStatus;
  odaProduct: OdaProduct | null;
};

export type ShoppingListCategory = {
  category: string;
  items: ShoppingListItem[];
};

export type ShoppingListResponse = {
  startDate: string;
  categories: ShoppingListCategory[];
};
