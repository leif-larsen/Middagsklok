namespace Middagsklok.Contracts.ShoppingList;

public record ShoppingListResponse(
    string WeekStartDate,
    IReadOnlyList<ShoppingListItemDto> Items);
