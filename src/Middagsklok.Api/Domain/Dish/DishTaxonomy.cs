namespace Middagsklok.Api.Domain.Dish;

public static class DishTaxonomy
{
    private static readonly DishTypeMetadata[] TypeMetadata =
    [
        new(DishType.Pasta, "Pasta", 100, 1.25, 1.1, false),
        new(DishType.RiceBowl, "Rice bowl", 110, 1.0, 1.0, false),
        new(DishType.Noodles, "Noodles", 120, 1.0, 1.0, false),
        new(DishType.SoupStew, "Soup & stew", 130, 0.95, 1.05, false),
        new(DishType.Salad, "Salad", 140, 0.9, 0.8, false),
        new(DishType.WrapTaco, "Wrap & taco", 150, 1.0, 1.2, false),
        new(DishType.PizzaPie, "Pizza & pie", 160, 0.8, 1.25, false),
        new(DishType.CasseroleBake, "Casserole & bake", 170, 0.85, 1.2, false),
        new(DishType.SandwichBurger, "Sandwich & burger", 180, 0.85, 1.2, false),
        new(DishType.ProteinVegPlate, "Protein & veg plate", 190, 1.0, 1.0, false),
        new(DishType.BreakfastDinner, "Breakfast for dinner", 200, 0.75, 1.1, false),
        new(DishType.SnackBoard, "Snack board", 210, 0.65, 1.15, false),
        new(DishType.Other, "Other", 900, 0.75, 0.75, true)
    ];

    private static readonly Dictionary<DishType, DishTypeMetadata> MetadataByType = TypeMetadata
        .ToDictionary(metadata => metadata.Value);

    private static readonly Dictionary<DishType, DishType> LegacyMapping = new()
    {
        [DishType.None] = DishType.Other,
        [DishType.Italian] = DishType.Pasta,
        [DishType.Asian] = DishType.RiceBowl,
        [DishType.Japanese] = DishType.RiceBowl,
        [DishType.Thai] = DishType.RiceBowl,
        [DishType.Chinese] = DishType.RiceBowl,
        [DishType.Mediterranean] = DishType.ProteinVegPlate,
        [DishType.Mexican] = DishType.WrapTaco,
        [DishType.Indian] = DishType.RiceBowl,
        [DishType.American] = DishType.SandwichBurger,
        [DishType.French] = DishType.ProteinVegPlate,
        [DishType.MiddleEastern] = DishType.ProteinVegPlate,
        [DishType.Vegetarian] = DishType.Other,
        [DishType.Vegan] = DishType.Other
    };

    private static readonly VibeTagMetadata[] VibeMetadata =
    [
        new("ComfortFood", "Comfort food", 100, 0.9, 1.35),
        new("QuickWeeknight", "Quick weeknight", 110, 1.2, 0.85),
        new("WeekendTreat", "Weekend treat", 120, 0.8, 1.4),
        new("LightFresh", "Light & fresh", 130, 1.05, 0.95),
        new("FamilyFriendly", "Family friendly", 140, 1.1, 1.05)
    ];
    private static readonly Dictionary<string, VibeTagMetadata> MetadataByVibeTag = VibeMetadata
        .ToDictionary(metadata => metadata.Value, StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyList<DishTypeMetadata> ReadOnlyTypeMetadata = Array.AsReadOnly(TypeMetadata);
    private static readonly IReadOnlyList<VibeTagMetadata> ReadOnlyVibeMetadata = Array.AsReadOnly(VibeMetadata);

    // Returns planner-facing dish type metadata.
    public static IReadOnlyList<DishTypeMetadata> GetDishTypes() => ReadOnlyTypeMetadata;

    // Returns planner-facing vibe tags with weekday/weekend multipliers.
    public static IReadOnlyList<VibeTagMetadata> GetVibeTags() => ReadOnlyVibeMetadata;

    // Tries to normalize a vibe tag value to its canonical form.
    public static bool TryNormalizeVibeTag(string? value, out string normalizedValue)
    {
        normalizedValue = string.Empty;

        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return false;
        }

        if (!MetadataByVibeTag.TryGetValue(trimmed, out var metadata))
        {
            return false;
        }

        normalizedValue = metadata.Value;
        return true;
    }

    // Maps legacy dishType values to planner-facing dish types.
    public static DishType NormalizeType(DishType value)
    {
        if (MetadataByType.ContainsKey(value))
        {
            return value;
        }

        if (LegacyMapping.TryGetValue(value, out var mapped))
        {
            return mapped;
        }

        return DishType.Other;
    }

    // Gets the default planner weight for a type and day category.
    public static double GetDefaultWeight(DishType value, DayOfWeek dayOfWeek)
    {
        var normalized = NormalizeType(value);
        if (!MetadataByType.TryGetValue(normalized, out var metadata))
        {
            return 1.0;
        }

        return IsWeekend(dayOfWeek)
            ? metadata.DefaultWeightWeekend
            : metadata.DefaultWeightWeekday;
    }

    // Gets the planner multiplier for a vibe tag and day category.
    public static double GetVibeWeightMultiplier(string value, DayOfWeek dayOfWeek)
    {
        if (!TryNormalizeVibeTag(value, out var normalizedValue))
        {
            return 1.0;
        }

        if (!MetadataByVibeTag.TryGetValue(normalizedValue, out var metadata))
        {
            return 1.0;
        }

        return IsWeekend(dayOfWeek)
            ? metadata.WeightMultiplierWeekend
            : metadata.WeightMultiplierWeekday;
    }

    // Indicates whether a day should use weekend multipliers.
    public static bool IsWeekend(DayOfWeek dayOfWeek) =>
        dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
}

public sealed record DishTypeMetadata(
    DishType Value,
    string Label,
    int DisplayOrder,
    double DefaultWeightWeekday,
    double DefaultWeightWeekend,
    bool IsFallback);

public sealed record VibeTagMetadata(
    string Value,
    string Label,
    int DisplayOrder,
    double WeightMultiplierWeekday,
    double WeightMultiplierWeekend);
