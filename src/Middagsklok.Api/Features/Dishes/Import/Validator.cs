using Middagsklok.Api.Domain.Dish;
using Middagsklok.Api.Domain.Ingredient;

namespace Middagsklok.Api.Features.Dishes.Import;

internal sealed class Validator
{
    // Validates dish input and returns a normalized candidate with any failures.
    public ValidationResult Validate(DishInput? dish)
    {
        var failures = new List<Failure>();

        if (dish is null)
        {
            failures.Add(new Failure(null, "Dish is required."));
            return new ValidationResult(false, null, failures);
        }

        var dishName = dish.Name?.Trim();
        if (string.IsNullOrWhiteSpace(dishName))
        {
            failures.Add(new Failure(null, "Dish name is required."));
        }

        if (dish.ActiveMinutes < 0)
        {
            failures.Add(new Failure(dishName, "Active minutes must be >= 0."));
        }

        if (dish.TotalMinutes < 0)
        {
            failures.Add(new Failure(dishName, "Total minutes must be >= 0."));
        }

        var ingredientsInput = dish.Ingredients ?? Array.Empty<IngredientInput>();
        if (ingredientsInput.Count == 0)
        {
            failures.Add(new Failure(dishName, "Dish must contain at least one ingredient."));
        }

        var candidates = new List<IngredientCandidate>();
        var seenIngredients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sortOrder = 1;

        foreach (var ingredient in ingredientsInput)
        {
            var ingredientName = ingredient.Name?.Trim();
            if (string.IsNullOrWhiteSpace(ingredientName))
            {
                failures.Add(new Failure(dishName, "Ingredient name is required."));
                continue;
            }

            if (ingredient.Amount <= 0)
            {
                failures.Add(new Failure(dishName, "Ingredient amount must be > 0.", ingredientName));
                continue;
            }

            var normalizedIngredientName = NormalizeName(ingredientName);
            if (!seenIngredients.Add(normalizedIngredientName))
            {
                continue;
            }

            var category = MapCategory(ingredient.Category);
            var unit = MapUnit(ingredient.Unit) ?? DefaultUnitForCategory(category);

            candidates.Add(new IngredientCandidate(ingredientName, category, unit, ingredient.Amount, sortOrder));
            sortOrder++;
        }

        if (candidates.Count == 0 && ingredientsInput.Count > 0)
        {
            failures.Add(new Failure(dishName, "Dish must contain at least one valid ingredient."));
        }

        if (failures.Count > 0)
        {
            return new ValidationResult(false, null, failures);
        }

        var candidate = new DishCandidate(
            dishName!,
            dish.ActiveMinutes,
            dish.TotalMinutes,
            4,
            CuisineType.None,
            candidates);

        return new ValidationResult(true, candidate, Array.Empty<Failure>());
    }

    // Maps a raw category string to the domain category.
    private static IngredientCategory MapCategory(string? rawCategory)
    {
        var category = NormalizeToken(rawCategory);

        var mapped = category switch
        {
            "produce" => IngredientCategory.Produce,
            "meat" => IngredientCategory.Meat,
            "poultry" => IngredientCategory.Poultry,
            "fish" or "seafood" => IngredientCategory.Seafood,
            "dairy" => IngredientCategory.DairyAndEggs,
            "dry" => IngredientCategory.PastaAndGrains,
            "bakery" => IngredientCategory.Bakery,
            "canned" => IngredientCategory.CannedGoods,
            "frozen" => IngredientCategory.FrozenFoods,
            "condiment" or "sauce" => IngredientCategory.Condiments,
            "spice" or "spices" or "herb" or "herbs" => IngredientCategory.SpicesAndHerbs,
            "baking" => IngredientCategory.Baking,
            "oil" or "oils" or "vinegar" => IngredientCategory.OilsAndVinegars,
            "beverage" or "beverages" => IngredientCategory.Beverages,
            "snack" or "snacks" => IngredientCategory.Snacks,
            _ => IngredientCategory.Other
        };

        return mapped;
    }

    // Maps a raw unit string to the domain unit, returning null when unknown.
    private static Unit? MapUnit(string? rawUnit)
    {
        var unit = NormalizeToken(rawUnit);

        Unit? mapped = unit switch
        {
            "g" or "gram" or "grams" => Unit.G,
            "kg" or "kilogram" or "kilograms" => Unit.Kg,
            "ml" or "milliliter" or "milliliters" => Unit.Ml,
            "l" or "liter" or "liters" => Unit.L,
            "stk" or "pcs" or "pc" or "piece" or "pieces" => Unit.Pcs,
            _ => null
        };

        return mapped;
    }

    // Chooses a default unit when the input unit is unknown.
    private static Unit DefaultUnitForCategory(IngredientCategory category)
    {
        var mapped = category switch
        {
            IngredientCategory.Meat => Unit.G,
            IngredientCategory.Poultry => Unit.G,
            IngredientCategory.Seafood => Unit.G,
            IngredientCategory.PastaAndGrains => Unit.G,
            IngredientCategory.Baking => Unit.G,
            IngredientCategory.SpicesAndHerbs => Unit.G,
            IngredientCategory.OilsAndVinegars => Unit.Ml,
            IngredientCategory.Beverages => Unit.Ml,
            IngredientCategory.DairyAndEggs => Unit.Pcs,
            IngredientCategory.Produce => Unit.Pcs,
            IngredientCategory.Bakery => Unit.Pcs,
            IngredientCategory.CannedGoods => Unit.Pcs,
            IngredientCategory.FrozenFoods => Unit.Pcs,
            IngredientCategory.Condiments => Unit.Pcs,
            IngredientCategory.Snacks => Unit.Pcs,
            _ => Unit.Pcs
        };

        return mapped;
    }

    // Normalizes names for case-insensitive comparisons.
    private static string NormalizeName(string value) => value.Trim().ToUpperInvariant();

    // Normalizes tokens for case-insensitive mapping.
    private static string NormalizeToken(string? value) => value?.Trim().ToLowerInvariant() ?? string.Empty;
}

internal sealed record ValidationResult(bool IsValid, DishCandidate? Candidate, IReadOnlyList<Failure> Failures);

internal sealed record DishCandidate(
    string Name,
    int PrepTimeMinutes,
    int CookTimeMinutes,
    int Servings,
    CuisineType Cuisine,
    IReadOnlyList<IngredientCandidate> Ingredients);

internal sealed record IngredientCandidate(
    string Name,
    IngredientCategory Category,
    Unit Unit,
    double Amount,
    int SortOrder);
