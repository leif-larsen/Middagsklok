using Middagsklok.Api.Domain.Ingredient;

namespace Middagsklok.Api.Features.Ingredients.Update;

internal sealed class Validator
{
    // Validates the update request and returns a candidate with any failures.
    public ValidationResult Validate(string? id, Request? request)
    {
        var failures = new List<ValidationError>();
        var ingredientId = ParseId(id, failures);

        if (request is null)
        {
            failures.Add(new ValidationError(string.Empty, "Ingredient is required."));
            return new ValidationResult(false, null, failures);
        }

        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            failures.Add(new ValidationError(ToFieldName(nameof(Request.Name)), "Ingredient name is required."));
        }

        var categoryResult = MapCategory(request.Category);
        if (!categoryResult.IsValid)
        {
            failures.Add(new ValidationError(
                ToFieldName(nameof(Request.Category)),
                categoryResult.ErrorMessage));
        }

        var unitResult = MapUnit(request.DefaultUnit);
        if (!unitResult.IsValid)
        {
            failures.Add(new ValidationError(
                ToFieldName(nameof(Request.DefaultUnit)),
                unitResult.ErrorMessage));
        }

        if (failures.Count > 0)
        {
            return new ValidationResult(false, null, failures);
        }

        var candidate = new IngredientCandidate(
            ingredientId,
            name!,
            categoryResult.Value,
            unitResult.Value);

        return new ValidationResult(true, candidate, Array.Empty<ValidationError>());
    }

    // Parses the ingredient identifier and adds failures on invalid values.
    private static Guid ParseId(string? rawId, ICollection<ValidationError> failures)
    {
        if (string.IsNullOrWhiteSpace(rawId) || !Guid.TryParse(rawId, out var parsed))
        {
            failures.Add(new ValidationError(ToFieldName(nameof(Ingredient.Id)), "Ingredient id is invalid."));
            return Guid.Empty;
        }

        return parsed;
    }

    // Maps a raw category string to the domain category result.
    private static CategoryParseResult MapCategory(string? rawCategory)
    {
        var trimmed = rawCategory?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            var requiredMessage = "Category is required.";
            return CategoryParseResult.Invalid(requiredMessage);
        }

        if (!Enum.TryParse<IngredientCategory>(trimmed, true, out var parsed)
            || !Enum.IsDefined(typeof(IngredientCategory), parsed))
        {
            var allowed = string.Join(", ", Enum.GetNames<IngredientCategory>());
            return CategoryParseResult.Invalid($"Category must be one of: {allowed}.");
        }

        return new CategoryParseResult(true, parsed, string.Empty);
    }

    // Maps a raw unit string to the domain unit result.
    private static UnitParseResult MapUnit(string? rawUnit)
    {
        var trimmed = rawUnit?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            var requiredMessage = "Default unit is required.";
            return UnitParseResult.Invalid(requiredMessage);
        }

        if (!Enum.TryParse<Unit>(trimmed, true, out var parsed)
            || !Enum.IsDefined(typeof(Unit), parsed))
        {
            var allowed = string.Join(", ", Enum.GetNames<Unit>());
            return UnitParseResult.Invalid($"Default unit must be one of: {allowed}.");
        }

        return new UnitParseResult(true, parsed, string.Empty);
    }

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
    IngredientCandidate? Candidate,
    IReadOnlyList<ValidationError> Errors);

internal sealed record IngredientCandidate(
    Guid Id,
    string Name,
    IngredientCategory Category,
    Unit DefaultUnit);

internal sealed record CategoryParseResult(
    bool IsValid,
    IngredientCategory Value,
    string ErrorMessage)
{
    // Creates an invalid parse result.
    public static CategoryParseResult Invalid(string message) => new(false, default, message);
}

internal sealed record UnitParseResult(
    bool IsValid,
    Unit Value,
    string ErrorMessage)
{
    // Creates an invalid parse result.
    public static UnitParseResult Invalid(string message) => new(false, default, message);
}
