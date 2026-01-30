namespace Middagsklok.Api.Features.ShoppingList.ByStartDate;

public sealed record Response(
    string StartDate,
    IEnumerable<ShoppingCategory> Categories);

public sealed record ShoppingCategory(
    string Category,
    IEnumerable<ShoppingItem> Items);

public sealed record ShoppingItem(
    string IngredientId,
    string Name,
    double Amount,
    string Unit);

public sealed record ErrorResponse(
    string Message,
    IReadOnlyList<ValidationError> Errors);

public sealed record ValidationError(string Field, string Message);
