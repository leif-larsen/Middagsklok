using Middagsklok.Api.Domain.Dish;

namespace Middagsklok.Api.Features.Dishes.Create;

internal sealed class Validator
{
    // Validates the create request and returns a candidate with any failures.
    public ValidationResult Validate(Request? request)
    {
        var failures = new List<ValidationError>();

        if (request is null)
        {
            failures.Add(new ValidationError(string.Empty, "Dish is required."));
            return new ValidationResult(false, null, failures);
        }

        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            failures.Add(new ValidationError(ToFieldName(nameof(Request.Name)), "Dish name is required."));
        }

        if (request.PrepMinutes < 0)
        {
            failures.Add(new ValidationError(ToFieldName(nameof(Request.PrepMinutes)), "Prep minutes must be >= 0."));
        }

        if (request.CookMinutes < 0)
        {
            failures.Add(new ValidationError(ToFieldName(nameof(Request.CookMinutes)), "Cook minutes must be >= 0."));
        }

        if (request.Serves < 0)
        {
            failures.Add(new ValidationError(ToFieldName(nameof(Request.Serves)), "Servings must be >= 0."));
        }

        var cuisineResult = MapCuisine(request.Cuisine);
        if (!cuisineResult.IsValid)
        {
            failures.Add(new ValidationError(
                ToFieldName(nameof(Request.Cuisine)),
                cuisineResult.ErrorMessage));
        }

        var vibeTagResult = ParseVibeTags(request.VibeTags);
        failures.AddRange(vibeTagResult.Errors);

        var ingredientsInput = request.Ingredients ?? Array.Empty<IngredientInput>();
        if (ingredientsInput.Count == 0)
        {
            failures.Add(new ValidationError(ToFieldName(nameof(Request.Ingredients)), "Dish must contain at least one ingredient."));
        }

        var candidates = new List<IngredientCandidate>();
        var seenIngredients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sortOrder = 1;

        for (var index = 0; index < ingredientsInput.Count; index++)
        {
            var ingredient = ingredientsInput[index];
            if (ingredient is null)
            {
                failures.Add(new ValidationError(BuildIngredientField(index), "Ingredient is required."));
                continue;
            }

            var rawId = ingredient.Id?.Trim();
            var hasId = !string.IsNullOrWhiteSpace(rawId);
            Guid? id = null;
            if (hasId)
            {
                if (!Guid.TryParse(rawId, out var parsedId))
                {
                    failures.Add(new ValidationError(
                        BuildIngredientField(index, nameof(IngredientInput.Id)),
                        "Ingredient id is invalid."));
                    continue;
                }

                if (parsedId == Guid.Empty)
                {
                    failures.Add(new ValidationError(
                        BuildIngredientField(index, nameof(IngredientInput.Id)),
                        "Ingredient id is invalid."));
                    continue;
                }

                id = parsedId;
            }

            var ingredientName = ingredient.Name?.Trim();
            if (!hasId && string.IsNullOrWhiteSpace(ingredientName))
            {
                failures.Add(new ValidationError(BuildIngredientField(index), "Ingredient id or name is required."));
                continue;
            }

            if (ingredient.Amount <= 0)
            {
                failures.Add(new ValidationError(
                    BuildIngredientField(index, nameof(IngredientInput.Amount)),
                    "Ingredient amount must be > 0."));
                continue;
            }

            var key = hasId
                ? $"id:{id}"
                : $"name:{NormalizeName(ingredientName!)}";
            if (!seenIngredients.Add(key))
            {
                continue;
            }

            var candidate = new IngredientCandidate(id, hasId ? null : ingredientName, ingredient.Amount, sortOrder, index);
            candidates.Add(candidate);
            sortOrder++;
        }

        if (candidates.Count == 0 && ingredientsInput.Count > 0)
        {
            failures.Add(new ValidationError(
                ToFieldName(nameof(Request.Ingredients)),
                "Dish must contain at least one valid ingredient."));
        }

        if (failures.Count > 0)
        {
            return new ValidationResult(false, null, failures);
        }

        var candidateDish = new DishCandidate(
            name!,
            cuisineResult.Value,
            request.PrepMinutes,
            request.CookMinutes,
            request.Serves,
            NormalizeInstructions(request.Instructions),
            request.IsSeafood,
            request.IsVegetarian,
            request.IsVegan,
            vibeTagResult.Values,
            candidates);

        return new ValidationResult(true, candidateDish, Array.Empty<ValidationError>());
    }

    // Maps a raw cuisine string to the domain cuisine type.
    private static CuisineParseResult MapCuisine(string? rawCuisine)
    {
        var trimmed = rawCuisine?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return CuisineParseResult.Valid(CuisineType.Other);
        }

        if (!Enum.TryParse<CuisineType>(trimmed, true, out var parsed)
            || !Enum.IsDefined(typeof(CuisineType), parsed))
        {
            var allowed = string.Join(", ", DishTaxonomy.GetDishTypes().Select(type => type.Value.ToString()));
            return CuisineParseResult.Invalid($"Cuisine must be one of: {allowed}.");
        }

        if (!DishTaxonomy.GetDishTypes().Any(type => type.Value == parsed))
        {
            var allowed = string.Join(", ", DishTaxonomy.GetDishTypes().Select(type => type.Value.ToString()));
            return CuisineParseResult.Invalid($"Cuisine must be one of: {allowed}.");
        }

        var normalized = DishTaxonomy.NormalizeType(parsed);
        return CuisineParseResult.Valid(normalized);
    }

    // Validates and normalizes planner vibe tags.
    private static VibeTagParseResult ParseVibeTags(IReadOnlyList<string>? rawVibeTags)
    {
        var values = rawVibeTags ?? Array.Empty<string>();
        if (values.Count == 0)
        {
            return new VibeTagParseResult(Array.Empty<string>(), Array.Empty<ValidationError>());
        }

        var normalizedValues = new List<string>();
        var failures = new List<ValidationError>();
        var seenValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var allowed = string.Join(", ", DishTaxonomy.GetVibeTags().Select(tag => tag.Value));

        for (var index = 0; index < values.Count; index++)
        {
            var rawValue = values[index];
            if (!DishTaxonomy.TryNormalizeVibeTag(rawValue, out var normalizedValue))
            {
                failures.Add(new ValidationError(
                    BuildVibeTagField(index),
                    $"Vibe tag must be one of: {allowed}."));
                continue;
            }

            if (!seenValues.Add(normalizedValue))
            {
                continue;
            }

            normalizedValues.Add(normalizedValue);
        }

        return new VibeTagParseResult(normalizedValues, failures);
    }

    // Normalizes free-form instructions input.
    private static string? NormalizeInstructions(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    // Builds the field name for an ingredient property.
    private static string BuildIngredientField(int index, string? property = null)
    {
        var prefix = $"{ToFieldName(nameof(Request.Ingredients))}[{index}]";

        if (string.IsNullOrWhiteSpace(property))
        {
            return prefix;
        }

        return $"{prefix}.{ToFieldName(property)}";
    }

    // Builds the field name for a vibe tag value.
    private static string BuildVibeTagField(int index) =>
        $"{ToFieldName(nameof(Request.VibeTags))}[{index}]";

    // Normalizes names for case-insensitive comparisons.
    private static string NormalizeName(string value) => value.Trim().ToUpperInvariant();

    // Converts property names to camelCase field names.
    private static string ToFieldName(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return string.Empty;
        }

        if (propertyName.Length == 1)
        {
            return propertyName.ToLowerInvariant();
        }

        var first = char.ToLowerInvariant(propertyName[0]);

        return $"{first}{propertyName[1..]}";
    }
}

internal sealed record ValidationResult(
    bool IsValid,
    DishCandidate? Candidate,
    IReadOnlyList<ValidationError> Errors);

internal sealed record DishCandidate(
    string Name,
    CuisineType Cuisine,
    int PrepTimeMinutes,
    int CookTimeMinutes,
    int Servings,
    string? Instructions,
    bool IsSeafood,
    bool IsVegetarian,
    bool IsVegan,
    IReadOnlyList<string> VibeTags,
    IReadOnlyList<IngredientCandidate> Ingredients);

internal sealed record IngredientCandidate(
    Guid? Id,
    string? Name,
    double Amount,
    int SortOrder,
    int Index);

internal sealed record CuisineParseResult(
    bool IsValid,
    CuisineType Value,
    string ErrorMessage)
{
    public static CuisineParseResult Valid(CuisineType value) => new(true, value, string.Empty);

    public static CuisineParseResult Invalid(string message) => new(false, default, message);
}

internal sealed record VibeTagParseResult(
    IReadOnlyList<string> Values,
    IReadOnlyList<ValidationError> Errors);
