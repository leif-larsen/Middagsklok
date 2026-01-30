using Middagsklok.Api.Domain.Dish;

namespace Middagsklok.Api.Features.Dishes.Delete;

internal sealed class Validator
{
    // Validates the dish identifier for deletion.
    public ValidationResult Validate(string? id)
    {
        var failures = new List<ValidationError>();
        var dishId = ParseId(id, failures);

        if (failures.Count > 0)
        {
            return new ValidationResult(false, Guid.Empty, failures);
        }

        return new ValidationResult(true, dishId, Array.Empty<ValidationError>());
    }

    // Parses the dish identifier and adds failures on invalid values.
    private static Guid ParseId(string? rawId, ICollection<ValidationError> failures)
    {
        if (string.IsNullOrWhiteSpace(rawId) || !Guid.TryParse(rawId, out var parsed))
        {
            failures.Add(new ValidationError(ToFieldName(nameof(Dish.Id)), "Dish id is invalid."));
            return Guid.Empty;
        }

        if (parsed == Guid.Empty)
        {
            failures.Add(new ValidationError(ToFieldName(nameof(Dish.Id)), "Dish id is invalid."));
            return Guid.Empty;
        }

        return parsed;
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
    Guid DishId,
    IReadOnlyList<ValidationError> Errors);
