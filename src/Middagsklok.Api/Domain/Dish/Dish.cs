using Middagsklok.Api.Domain.Ingredient;

namespace Middagsklok.Api.Domain.Dish;

public class Dish(
    string name,
    DishType dishType,
    int prepTimeMinutes,
    int cookTimeMinutes,
    int servings,
    string? instructions,
    bool isSeafood,
    bool isVegetarian,
    bool isVegan,
    IEnumerable<DishIngredient> ingredients,
    IEnumerable<string>? vibeTags = null) : BaseEntity
{
    private readonly List<DishIngredient> _ingredients = ingredients.ToList();
    private readonly List<string> _vibeTags = NormalizeVibeTags(vibeTags);

    // Required by EF Core.
    private Dish(
        string name,
        DishType dishType,
        int prepTimeMinutes,
        int cookTimeMinutes,
        int servings,
        string? instructions)
        : this(name, dishType, prepTimeMinutes, cookTimeMinutes, servings, instructions, false, false, false, Array.Empty<DishIngredient>(), null)
    {
    }

    public string Name { get; private set; } = name.Trim();
    public DishType DishType { get; private set; } = dishType;
    public int PrepTimeMinutes { get; private set; } = prepTimeMinutes;
    public int CookTimeMinutes { get; private set; } = cookTimeMinutes;
    public int Servings { get; private set; } = servings;
    public string? Instructions { get; private set; } = NormalizeInstructions(instructions);
    public bool IsSeafood { get; private set; } = isSeafood;
    public bool IsVegetarian { get; private set; } = isVegetarian;
    public bool IsVegan { get; private set; } = isVegan;
    public IReadOnlyList<DishIngredient> Ingredients => _ingredients;
    public IReadOnlyList<string> VibeTags => _vibeTags;
    public DateTime? RetiredAt { get; private set; }

    public bool IsRetired => RetiredAt is not null;

    public int TotalTimeMinutes => PrepTimeMinutes + CookTimeMinutes;

    // Updates dish details and ingredients.
    public void Update(
        string name,
        DishType dishType,
        int prepTimeMinutes,
        int cookTimeMinutes,
        int servings,
        string? instructions,
        bool isSeafood,
        bool isVegetarian,
        bool isVegan,
        IEnumerable<DishIngredient> ingredients,
        IEnumerable<string>? vibeTags)
    {
        Name = name.Trim();
        DishType = dishType;
        PrepTimeMinutes = prepTimeMinutes;
        CookTimeMinutes = cookTimeMinutes;
        Servings = servings;
        Instructions = NormalizeInstructions(instructions);
        IsSeafood = isSeafood;
        IsVegetarian = isVegetarian;
        IsVegan = isVegan;

        _ingredients.Clear();
        _ingredients.AddRange(ingredients);
        _vibeTags.Clear();
        _vibeTags.AddRange(NormalizeVibeTags(vibeTags));

        Touch();
    }

    // Retires the dish so it is excluded from weekly plan generation.
    public void Retire()
    {
        RetiredAt ??= DateTime.UtcNow;
        Touch();
    }

    // Restores the dish to active status.
    public void Unretire()
    {
        RetiredAt = null;
        Touch();
    }

    // Normalizes instructions for persistence.
    private static string? NormalizeInstructions(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    // Normalizes vibe tags for persistence, silently dropping unknown values.
    private static List<string> NormalizeVibeTags(IEnumerable<string>? tags)
    {
        if (tags is null)
        {
            return [];
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalized = new List<string>();

        foreach (var tag in tags)
        {
            if (!DishTaxonomy.TryNormalizeVibeTag(tag, out var canonicalTag))
            {
                continue;
            }

            if (seen.Add(canonicalTag))
            {
                normalized.Add(canonicalTag);
            }
        }

        return normalized;
    }
}

public record DishIngredient(
    Guid IngredientId,
    double Quantity,
    Unit Unit,
    string? Note = null,
    int? SortOrder = null,
    IngredientScaling Scaling = IngredientScaling.PerDish,
    int? PersonCount = null)
{
    // Resolves the amount actually needed when the dish is cooked for the given number of servings.
    public double AmountFor(int servings) =>
        Scaling switch
        {
            IngredientScaling.PerServing => Quantity * servings,
            IngredientScaling.PerPerson => Quantity * (PersonCount ?? 1),
            _ => Quantity
        };
}

// Determines how a dish ingredient's stored quantity responds to the number of servings.
public enum IngredientScaling
{
    // The stored quantity is the whole-dish amount and never scales.
    // This is the default so that pre-scaling data keeps its original meaning.
    PerDish,

    // The stored quantity is per serving and is multiplied by the servings cooked.
    PerServing,

    // The stored quantity is per eater, for ingredients only some of the table eat.
    // Deliberately does not grow when guests are added.
    PerPerson
}

public enum DishType
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
