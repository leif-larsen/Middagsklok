namespace Middagsklok.Contracts.Dishes.Details;

public record UpdateDishIngredientItem(
    string Name,
    string Category,
    decimal Amount,
    string Unit,
    bool Optional);

public record UpdateDishRequest(
    string Name,
    int ActiveMinutes,
    int TotalMinutes,
    int KidRating,
    int FamilyRating,
    bool IsPescetarian,
    bool HasOptionalMeatVariant,
    IReadOnlyList<UpdateDishIngredientItem> Ingredients);
