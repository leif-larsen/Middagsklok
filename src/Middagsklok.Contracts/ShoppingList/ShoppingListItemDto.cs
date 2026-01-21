namespace Middagsklok.Contracts.ShoppingList;

public record ShoppingListItemDto(
    string IngredientName,
    string Category,
    decimal Amount,
    string Unit);
