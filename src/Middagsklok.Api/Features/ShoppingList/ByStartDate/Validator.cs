using System.Globalization;
using Middagsklok.Api.Domain.WeeklyPlan;

namespace Middagsklok.Api.Features.ShoppingList.ByStartDate;

internal sealed class Validator
{
    // Validates the request parameters for shopping list retrieval.
    public ValidationResult Validate(string? startDateValue)
    {
        var failures = new List<ValidationError>();

        if (!TryParseDate(startDateValue, out var startDate))
        {
            failures.Add(new ValidationError(ToFieldName(nameof(WeeklyPlan.StartDate)), "Start date is invalid."));
        }

        if (failures.Count > 0)
        {
            return new ValidationResult(false, DateOnly.MinValue, failures);
        }

        return new ValidationResult(true, startDate, Array.Empty<ValidationError>());
    }

    // Parses date strings into DateOnly values.
    private static bool TryParseDate(string? value, out DateOnly date)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            date = DateOnly.MinValue;
            return false;
        }

        return DateOnly.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);
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
    DateOnly StartDate,
    IReadOnlyList<ValidationError> Errors);
