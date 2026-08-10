using Middagsklok.Api.Domain.Dish;
using Middagsklok.Api.Domain.Ingredient;

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

        var dishTypeResult = MapDishType(request.DishType);
        if (!dishTypeResult.IsValid)
        {
            failures.Add(new ValidationError(
                ToFieldName(nameof(Request.DishType)),
                dishTypeResult.ErrorMessage));
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

            var portion = ParsePortion(ingredient, index);
            if (portion.Errors.Count > 0)
            {
                failures.AddRange(portion.Errors);
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
                index,
                portion.Unit,
                portion.Scaling,
                portion.PersonCount);
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
            dishTypeResult.Value,
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

    // Validates the unit and scaling fields of a single ingredient input.
    private static PortionParseResult ParsePortion(IngredientInput ingredient, int index)
    {
        var failures = new List<ValidationError>();

        Unit? unit = null;
        if (!string.IsNullOrWhiteSpace(ingredient.Unit))
        {
            if (PortionTaxonomy.TryNormalizeUnit(ingredient.Unit, out var parsedUnit))
            {
                unit = parsedUnit;
            }
            else
            {
                failures.Add(new ValidationError(
                    BuildIngredientField(index, nameof(IngredientInput.Unit)),
                    $"Ingredient unit must be one of: {PortionTaxonomy.DescribeAllowedUnits()}."));
            }
        }

        IngredientScaling? scaling = null;
        if (!string.IsNullOrWhiteSpace(ingredient.Scaling))
        {
            if (PortionTaxonomy.TryNormalizeScaling(ingredient.Scaling, out var parsedScaling))
            {
                scaling = parsedScaling;
            }
            else
            {
                failures.Add(new ValidationError(
                    BuildIngredientField(index, nameof(IngredientInput.Scaling)),
                    $"Ingredient scaling must be one of: {PortionTaxonomy.DescribeAllowedScalings()}."));
            }
        }

        // Cross-checks only apply when the caller states a scaling mode. Omitting it means
        // "leave the stored value alone", which the use case resolves against the existing row.
        if (scaling is IngredientScaling.PerPerson && ingredient.PersonCount is null or < 1)
        {
            failures.Add(new ValidationError(
                BuildIngredientField(index, nameof(IngredientInput.PersonCount)),
                "Person count must be >= 1 when scaling is PerPerson."));
        }

        if (scaling is not null and not IngredientScaling.PerPerson && ingredient.PersonCount is not null)
        {
            failures.Add(new ValidationError(
                BuildIngredientField(index, nameof(IngredientInput.PersonCount)),
                "Person count is only valid when scaling is PerPerson."));
        }

        return new PortionParseResult(unit, scaling, ingredient.PersonCount, failures);
    }

    // Maps a raw dish type string to the domain dish type.
    private static DishTypeParseResult MapDishType(string? rawDishType)
    {
        var dishTypes = DishTaxonomy.GetDishTypes();
        var allowed = string.Join(", ", dishTypes.Select(type => type.Value.ToString()));

        var trimmed = rawDishType?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return DishTypeParseResult.Valid(DishType.Other);
        }

        if (!Enum.TryParse<DishType>(trimmed, true, out var parsed)
            || !Enum.IsDefined(typeof(DishType), parsed))
        {
            return DishTypeParseResult.Invalid($"Dish type must be one of: {allowed}.");
        }

        if (!dishTypes.Any(type => type.Value == parsed))
        {
            return DishTypeParseResult.Invalid($"Dish type must be one of: {allowed}.");
        }

        var normalized = DishTaxonomy.NormalizeType(parsed);
        return DishTypeParseResult.Valid(normalized);
    }

    // Normalizes planner vibe tags, silently dropping any value outside the vocabulary.
    private static VibeTagParseResult ParseVibeTags(IReadOnlyList<string>? rawVibeTags)
    {
        var values = rawVibeTags ?? Array.Empty<string>();
        if (values.Count == 0)
        {
            return new VibeTagParseResult(Array.Empty<string>(), Array.Empty<ValidationError>());
        }

        var normalizedValues = new List<string>();
        var seenValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawValue in values)
        {
            if (!DishTaxonomy.TryNormalizeVibeTag(rawValue, out var normalizedValue))
            {
                continue;
            }

            if (!seenValues.Add(normalizedValue))
            {
                continue;
            }

            normalizedValues.Add(normalizedValue);
        }

        return new VibeTagParseResult(normalizedValues, Array.Empty<ValidationError>());
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
    Guid Id,
    string Name,
    DishType DishType,
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
    int Index,
    Unit? Unit = null,
    IngredientScaling? Scaling = null,
    int? PersonCount = null);

internal sealed record PortionParseResult(
    Unit? Unit,
    IngredientScaling? Scaling,
    int? PersonCount,
    IReadOnlyList<ValidationError> Errors);

internal sealed record DishTypeParseResult(
    bool IsValid,
    DishType Value,
    string ErrorMessage)
{
    public static DishTypeParseResult Valid(DishType value) => new(true, value, string.Empty);

    public static DishTypeParseResult Invalid(string message) => new(false, default, message);
}

internal sealed record VibeTagParseResult(
    IReadOnlyList<string> Values,
    IReadOnlyList<ValidationError> Errors);
