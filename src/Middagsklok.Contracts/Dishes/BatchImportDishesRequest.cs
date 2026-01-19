namespace Middagsklok.Contracts.Dishes;

public record DishIngredientItem(
    string Name,
    string Category,
    decimal Amount,
    string Unit,
    bool Optional);

public record DishImportItem(
    string Name,
    int ActiveMinutes,
    int TotalMinutes,
    int KidRating,
    int FamilyRating,
    bool IsPescetarian,
    bool HasOptionalMeatVariant,
    List<string>? Tags,
    List<DishIngredientItem> Ingredients);

public record BatchImportDishesRequest(List<DishImportItem> Dishes);
