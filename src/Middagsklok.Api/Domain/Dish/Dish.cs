using Middagsklok.Api.Domain.Ingredient;

namespace Middagsklok.Api.Domain.Dish;

public class Dish(
    string name,
    CuisineType cuisine,
    int prepTimeMinutes,
    int cookTimeMinutes,
    int servings,
    IEnumerable<DishIngredient> ingredients) : BaseEntity
{
    private readonly List<DishIngredient> _ingredients = ingredients.ToList();

    // Required by EF Core.
    private Dish(string name, CuisineType cuisine, int prepTimeMinutes, int cookTimeMinutes, int servings)
        : this(name, cuisine, prepTimeMinutes, cookTimeMinutes, servings, Array.Empty<DishIngredient>())
    {
    }

    public string Name { get; } = name.Trim();
    public CuisineType Cuisine { get; } = cuisine;
    public int PrepTimeMinutes { get; } = prepTimeMinutes;
    public int CookTimeMinutes { get; } = cookTimeMinutes;
    public int Servings { get; } = servings;
    public IReadOnlyList<DishIngredient> Ingredients => _ingredients;

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
