namespace Middagsklok.Contracts.Dishes;

public record BatchImportDishResultItem(
    string Name,
    string Status,
    string? DishId,
    string? Error);

public record BatchImportDishesResponse(
    int Total,
    int Created,
    int Skipped,
    int Failed,
    IReadOnlyList<BatchImportDishResultItem> Results);
