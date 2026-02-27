using System.Globalization;

namespace Middagsklok.Api.Features.WeeklyPlans.PlannedDishByDate;

internal sealed class Validator
{
    // Validates and parses the ISO-8601 date string.
    public ValidationResult Validate(string? date)
    {
        if (string.IsNullOrWhiteSpace(date))
        {
            return new ValidationResult(false, default, "Date is required.");
        }

        var canParse = DateOnly.TryParseExact(
            date,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsedDate);

        if (!canParse)
        {
            return new ValidationResult(false, default, "Date must be in ISO-8601 format (yyyy-MM-dd).");
        }

        return new ValidationResult(true, parsedDate, null);
    }
}

internal sealed record ValidationResult(
    bool IsValid,
    DateOnly Date,
    string? ErrorMessage);
