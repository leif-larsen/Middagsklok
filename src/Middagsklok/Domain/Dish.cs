namespace Middagsklok.Domain;

public record Dish(
    Guid Id,
    string Name,
    int ActiveMinutes,
    int TotalMinutes,
    int KidRating,
    int FamilyRating,
    bool IsPescetarian,
    bool HasOptionalMeatVariant,
    IReadOnlyList<DishIngredient> Ingredients);
