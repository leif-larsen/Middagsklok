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
    string Unit,
    IReadOnlyList<string> Dishes,
    bool IsPantryStaple,
    string OdaStatus,
    OdaProduct? OdaProduct);

// Present only when the ingredient maps to a concrete Oda product.
public sealed record OdaProduct(
    int ProductId,
    string ProductName,
    double PackQuantity,
    string PackUnit,
    int? SuggestedPackCount,
    bool Confirmed);

public sealed record ErrorResponse(
    string Message,
    IReadOnlyList<ValidationError> Errors);

public sealed record ValidationError(string Field, string Message);
