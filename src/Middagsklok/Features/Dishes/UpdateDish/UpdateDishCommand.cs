namespace Middagsklok.Features.Dishes.UpdateDish;

public record UpdateDishIngredientItem(
    string Name,
    string Category,
    decimal Amount,
    string Unit,
    bool Optional);

public record UpdateDishCommand(
    Guid DishId,
    string Name,
    int ActiveMinutes,
    int TotalMinutes,
    int KidRating,
    int FamilyRating,
    bool IsPescetarian,
    bool HasOptionalMeatVariant,
    IReadOnlyList<UpdateDishIngredientItem> Ingredients);

public record UpdateDishResult(
    Guid Id,
    DateTime UpdatedAt,
    IReadOnlyList<string> Warnings);
