namespace Middagsklok.Features.Dishes.Import;

public record AddDishCommand(
    string Name,
    int ActiveMinutes,
    int TotalMinutes,
    int KidRating,
    int FamilyRating,
    bool IsPescetarian,
    bool HasOptionalMeatVariant,
    List<string>? Tags,
    List<AddDishIngredientItem> Ingredients);

public record AddDishIngredientItem(
    string Name,
    string Category,
    decimal Amount,
    string Unit,
    bool Optional);

public record BatchImportDishesCommand(
    List<AddDishCommand> Dishes);

public record BatchImportResult(
    int Total,
    int Created,
    int Skipped,
    int Failed,
    IReadOnlyList<BatchImportDishResult> Results);

public record BatchImportDishResult(
    string Name,
    string Status,
    Guid? DishId,
    string? Error);
