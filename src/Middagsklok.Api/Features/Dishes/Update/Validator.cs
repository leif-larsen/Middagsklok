using Middagsklok.Api.Domain.Dish;

namespace Middagsklok.Api.Features.Dishes.Update;

internal sealed class Validator
{
    // Validates the update request and returns a candidate with any failures.
    public ValidationResult Validate(string? id, Request? request)
    {
        var failures = new List<ValidationError>();

        var dishId = ParseId(id, failures);
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
            Guid? ingredientId = null;
            if (hasId)
            {
                if (!Guid.TryParse(rawId, out var parsedId) || parsedId == Guid.Empty)
                {
                    failures.Add(new ValidationError(
                        BuildIngredientField(index, nameof(IngredientInput.Id)),
                        "Ingredient id is invalid."));
                    continue;
                }

                ingredientId = parsedId;
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
                ? $"id:{ingredientId}"
                : $"name:{NormalizeName(ingredientName!)}";
            if (!seenIngredients.Add(key))
            {
                continue;
            }

            var candidate = new IngredientCandidate(
                ingredientId,
                hasId ? null : ingredientName,
                ingredient.Amount,
                sortOrder,
                index);
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
            dishId,
            name!,
            cuisineResult.Value,
            request.PrepMinutes,
            request.CookMinutes,
            request.Serves,
            NormalizeInstructions(request.Instructions),
            request.IsSeafood,
            request.IsVegetarian,
            request.IsVegan,
            candidates);

        return new ValidationResult(true, candidateDish, Array.Empty<ValidationError>());
    }

    // Parses the dish id and records failures when invalid.
    private static Guid ParseId(string? rawId, ICollection<ValidationError> failures)
    {
        if (Guid.TryParse(rawId, out var parsedId) && parsedId != Guid.Empty)
        {
            return parsedId;
        }

        failures.Add(new ValidationError(ToFieldName(nameof(Dish.Id)), "Dish id is invalid."));
        return Guid.Empty;
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
            var allowed = string.Join(", ", Enum.GetNames<CuisineType>().Where(value => value != nameof(CuisineType.None)));
            return CuisineParseResult.Invalid($"Cuisine must be one of: {allowed}.");
        }

        var normalized = parsed is CuisineType.None ? CuisineType.Other : parsed;
        return CuisineParseResult.Valid(normalized);
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
    Guid Id,
    string Name,
    CuisineType Cuisine,
    int PrepTimeMinutes,
    int CookTimeMinutes,
    int Servings,
    string? Instructions,
    bool IsSeafood,
    bool IsVegetarian,
    bool IsVegan,
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
