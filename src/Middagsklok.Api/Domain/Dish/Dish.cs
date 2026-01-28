using Middagsklok.Api.Domain.Ingredient;

namespace Middagsklok.Api.Domain.Dish;

public class Dish(
    string name,
    CuisineType cuisine,
    int prepTimeMinutes,
    int cookTimeMinutes,
    int servings,
    List<DishIngredient> ingredients) : BaseEntity
{
    public string Name { get; } = name.Trim();
    public CuisineType Cuisine { get; } = cuisine;
    public int PrepTimeMinutes { get; } = prepTimeMinutes;
    public int CookTimeMinutes { get; } = cookTimeMinutes;
    public int Servings { get; } = servings;
    public IReadOnlyList<DishIngredient> Ingredients { get; } = ingredients;

    public int TotalTimeMinutes => PrepTimeMinutes + CookTimeMinutes;
}

public record DishIngredient(
    Guid IngredientId,
    double Quantity,
    Unit Unit,
    string? Note = null,
    int? SortOrder = null);

public enum CuisineType
{
    None,
    Italian,
    Asian,
    Mediterranean,
    Mexican,
    Indian,
    American,
    French,
    MiddleEastern,
    Japanese,
    Thai,
    Chinese,
    Vegetarian,
    Vegan,
    Other
}
