namespace Middagsklok.Features.ShoppingList.GenerateForWeek;

public record ShoppingListItem(
    string IngredientName,
    string Category,
    decimal Amount,
    string Unit);
