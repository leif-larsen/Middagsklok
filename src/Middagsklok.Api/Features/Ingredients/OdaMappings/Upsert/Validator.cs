using Middagsklok.Api.Domain.Ingredient;

namespace Middagsklok.Api.Features.Ingredients.OdaMappings.Upsert;

internal sealed class Validator
{
    // Validates the upsert request and returns a candidate with any failures.
    public ValidationResult Validate(string? id, Request? request)
    {
        var failures = new List<ValidationError>();
        var ingredientId = ParseId(id, failures);

        if (request is null)
        {
            failures.Add(new ValidationError(string.Empty, "Mapping is required."));
            return new ValidationResult(false, null, failures);
        }

        if (request.NotAvailable)
        {
            if (request.ProductId is not null || !string.IsNullOrWhiteSpace(request.ProductName))
            {
                failures.Add(new ValidationError(
                    ToFieldName(nameof(Request.NotAvailable)),
                    "A mapping cannot both name a product and be marked not available."));
            }

            return failures.Count > 0
                ? new ValidationResult(false, null, failures)
                : new ValidationResult(true, MappingCandidate.NotAvailable(ingredientId), Array.Empty<ValidationError>());
        }

        if (request.ProductId is null or <= 0)
        {
            failures.Add(new ValidationError(ToFieldName(nameof(Request.ProductId)), "Product id must be a positive integer."));
        }

        var productName = request.ProductName?.Trim();
        if (string.IsNullOrWhiteSpace(productName))
        {
            failures.Add(new ValidationError(ToFieldName(nameof(Request.ProductName)), "Product name is required."));
        }

        if (request.PackQuantity is null or <= 0)
        {
            failures.Add(new ValidationError(ToFieldName(nameof(Request.PackQuantity)), "Pack quantity must be greater than zero."));
        }

        var unitResult = MapUnit(request.PackUnit);
        if (!unitResult.IsValid)
        {
            failures.Add(new ValidationError(ToFieldName(nameof(Request.PackUnit)), unitResult.ErrorMessage));
        }

        if (failures.Count > 0)
        {
            return new ValidationResult(false, null, failures);
        }

        var candidate = MappingCandidate.ForProduct(
            ingredientId,
            request.ProductId!.Value,
            productName!,
            request.PackQuantity!.Value,
            unitResult.Value,
            request.Confirmed);

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

    // Maps a raw unit string to the domain unit result.
    private static UnitParseResult MapUnit(string? rawUnit)
    {
        var trimmed = rawUnit?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return UnitParseResult.Invalid("Pack unit is required.");
        }

        if (!Enum.TryParse<Unit>(trimmed, true, out var parsed)
            || !Enum.IsDefined(typeof(Unit), parsed))
        {
            var allowed = string.Join(", ", Enum.GetNames<Unit>());
            return UnitParseResult.Invalid($"Pack unit must be one of: {allowed}.");
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
    MappingCandidate? Candidate,
    IReadOnlyList<ValidationError> Errors);

internal sealed record MappingCandidate(
    Guid IngredientId,
    bool IsNotAvailable,
    int ProductId,
    string ProductName,
    double PackQuantity,
    Unit PackUnit,
    bool Confirmed)
{
    // Creates a candidate for a concrete Oda product.
    public static MappingCandidate ForProduct(
        Guid ingredientId,
        int productId,
        string productName,
        double packQuantity,
        Unit packUnit,
        bool confirmed) =>
        new(ingredientId, false, productId, productName, packQuantity, packUnit, confirmed);

    // Creates a candidate that marks the ingredient as not stocked at Oda.
    public static MappingCandidate NotAvailable(Guid ingredientId) =>
        new(ingredientId, true, 0, string.Empty, 0, default, true);
}

internal sealed record UnitParseResult(
    bool IsValid,
    Unit Value,
    string ErrorMessage)
{
    // Creates an invalid parse result.
    public static UnitParseResult Invalid(string message) => new(false, default, message);
}
