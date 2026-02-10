using Middagsklok.Api.Domain.Ingredient;

namespace Middagsklok.Api.Domain.Dish;

public class Dish(
    string name,
    CuisineType cuisine,
    int prepTimeMinutes,
    int cookTimeMinutes,
    int servings,
    string? instructions,
    bool isSeafood,
    bool isVegetarian,
    bool isVegan,
    IEnumerable<DishIngredient> ingredients) : BaseEntity
{
    private readonly List<DishIngredient> _ingredients = ingredients.ToList();

    // Required by EF Core.
    private Dish(
        string name,
        CuisineType cuisine,
        int prepTimeMinutes,
        int cookTimeMinutes,
        int servings,
        string? instructions)
        : this(name, cuisine, prepTimeMinutes, cookTimeMinutes, servings, instructions, false, false, false, Array.Empty<DishIngredient>())
    {
    }

    public string Name { get; private set; } = name.Trim();
    public CuisineType Cuisine { get; private set; } = cuisine;
    public int PrepTimeMinutes { get; private set; } = prepTimeMinutes;
    public int CookTimeMinutes { get; private set; } = cookTimeMinutes;
    public int Servings { get; private set; } = servings;
    public string? Instructions { get; private set; } = NormalizeInstructions(instructions);
    public bool IsSeafood { get; private set; } = isSeafood;
    public bool IsVegetarian { get; private set; } = isVegetarian;
    public bool IsVegan { get; private set; } = isVegan;
    public IReadOnlyList<DishIngredient> Ingredients => _ingredients;

    public int TotalTimeMinutes => PrepTimeMinutes + CookTimeMinutes;

    // Updates dish details and ingredients.
    public void Update(
        string name,
        CuisineType cuisine,
        int prepTimeMinutes,
        int cookTimeMinutes,
        int servings,
        string? instructions,
        bool isSeafood,
        bool isVegetarian,
        bool isVegan,
        IEnumerable<DishIngredient> ingredients)
    {
        Name = name.Trim();
        Cuisine = cuisine;
        PrepTimeMinutes = prepTimeMinutes;
        CookTimeMinutes = cookTimeMinutes;
        Servings = servings;
        Instructions = NormalizeInstructions(instructions);
        IsSeafood = isSeafood;
        IsVegetarian = isVegetarian;
        IsVegan = isVegan;

        _ingredients.Clear();
        _ingredients.AddRange(ingredients);

        Touch();
    }

    // Normalizes instructions for persistence.
    private static string? NormalizeInstructions(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
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
    Pasta,
    RiceBowl,
    Noodles,
    SoupStew,
    Salad,
    WrapTaco,
    PizzaPie,
    CasseroleBake,
    SandwichBurger,
    ProteinVegPlate,
    BreakfastDinner,
    SnackBoard,
    Other,

    // Legacy values kept for backwards compatibility with persisted dish data.
    Italian,
    Asian,
    Japanese,
    Thai,
    Chinese,
    Mediterranean,
    Mexican,
    Indian,
    American,
    French,
    MiddleEastern,
    Vegetarian,
    Vegan
}
