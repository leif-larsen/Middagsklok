using Middagsklok.Api.Domain.Ingredient;

namespace Middagsklok.Api.Domain.Dish;

// Parsing and display helpers for the units and scaling modes carried by a dish ingredient.
public static class PortionTaxonomy
{
    private static readonly UnitMetadata[] UnitMetadataValues =
    [
        new(Unit.G, "g", "Gram", 100),
        new(Unit.Kg, "kg", "Kilogram", 110),
        new(Unit.Ml, "ml", "Millilitre", 120),
        new(Unit.L, "l", "Litre", 130),
        new(Unit.Pcs, "pcs", "Pieces", 140),
        new(Unit.Pack, "pk", "Packages", 150)
    ];

    private static readonly ScalingMetadata[] ScalingMetadataValues =
    [
        new(IngredientScaling.PerDish, "Per dish", "Fixed amount regardless of servings", 100),
        new(IngredientScaling.PerServing, "Per serving", "Multiplied by the servings cooked", 110),
        new(IngredientScaling.PerPerson, "Per person", "Multiplied by personCount only", 120)
    ];

    private static readonly Dictionary<string, Unit> UnitsByName = BuildUnitLookup();

    private static readonly Dictionary<Unit, UnitMetadata> MetadataByUnit = UnitMetadataValues
        .ToDictionary(metadata => metadata.Value);

    private static readonly IReadOnlyList<UnitMetadata> ReadOnlyUnitMetadata =
        Array.AsReadOnly(UnitMetadataValues);

    private static readonly IReadOnlyList<ScalingMetadata> ReadOnlyScalingMetadata =
        Array.AsReadOnly(ScalingMetadataValues);

    // Returns the units available to a dish ingredient.
    public static IReadOnlyList<UnitMetadata> GetUnits() => ReadOnlyUnitMetadata;

    // Returns the scaling modes available to a dish ingredient.
    public static IReadOnlyList<ScalingMetadata> GetScalings() => ReadOnlyScalingMetadata;

    // Tries to resolve a raw unit value, accepting both the enum name and the display abbreviation.
    public static bool TryNormalizeUnit(string? value, out Unit normalizedValue)
    {
        normalizedValue = Unit.Pcs;

        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return false;
        }

        if (!UnitsByName.TryGetValue(trimmed, out var unit))
        {
            return false;
        }

        normalizedValue = unit;

        return true;
    }

    // Tries to resolve a raw scaling value to its canonical form.
    public static bool TryNormalizeScaling(string? value, out IngredientScaling normalizedValue)
    {
        normalizedValue = IngredientScaling.PerDish;

        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return false;
        }

        if (!Enum.TryParse<IngredientScaling>(trimmed, true, out var parsed)
            || !Enum.IsDefined(parsed))
        {
            return false;
        }

        normalizedValue = parsed;

        return true;
    }

    // Formats a unit for human-readable ingredient labels.
    public static string FormatUnit(Unit value) =>
        MetadataByUnit.TryGetValue(value, out var metadata)
            ? metadata.Abbreviation
            : string.Empty;

    // Lists the allowed unit values for validation messages.
    public static string DescribeAllowedUnits() =>
        string.Join(", ", UnitMetadataValues.Select(metadata => metadata.Value.ToString()));

    // Lists the allowed scaling values for validation messages.
    public static string DescribeAllowedScalings() =>
        string.Join(", ", ScalingMetadataValues.Select(metadata => metadata.Value.ToString()));

    // Builds the case-insensitive lookup covering enum names and display abbreviations.
    private static Dictionary<string, Unit> BuildUnitLookup()
    {
        var lookup = new Dictionary<string, Unit>(StringComparer.OrdinalIgnoreCase);

        foreach (var metadata in UnitMetadataValues)
        {
            lookup[metadata.Value.ToString()] = metadata.Value;
            lookup[metadata.Abbreviation] = metadata.Value;
        }

        return lookup;
    }
}

public sealed record UnitMetadata(
    Unit Value,
    string Abbreviation,
    string Label,
    int DisplayOrder);

public sealed record ScalingMetadata(
    IngredientScaling Value,
    string Label,
    string Description,
    int DisplayOrder);
