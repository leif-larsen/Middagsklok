namespace Middagsklok.Contracts.Dishes.Details;

public record DishDetailsIngredientItem(
    string Name,
    string Category,
    decimal Amount,
    string Unit,
    bool Optional);

public record DishDetailsResponse(
    string Id,
    string Name,
    int ActiveMinutes,
    int TotalMinutes,
    int KidRating,
    int FamilyRating,
    bool IsPescetarian,
    bool HasOptionalMeatVariant,
    IReadOnlyList<DishDetailsIngredientItem> Ingredients);
