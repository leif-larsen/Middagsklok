using System.Globalization;
using Middagsklok.Api.Domain.WeeklyPlan;

namespace Middagsklok.Api.Features.WeeklyPlans.Upsert;

internal sealed class Validator
{
    // Validates the upsert request and returns a candidate with any failures.
    public ValidationResult Validate(string? startDateValue, Request? request)
    {
        var failures = new List<ValidationError>();

        if (!TryParseDate(startDateValue, out var startDate))
        {
            failures.Add(new ValidationError(ToFieldName(nameof(WeeklyPlan.StartDate)), "Start date is invalid."));
        }

        if (request is null)
        {
            failures.Add(new ValidationError(string.Empty, "Weekly plan is required."));
            return new ValidationResult(false, null, failures);
        }

        var daysInput = request.Days ?? Array.Empty<PlannedDayInput>();
        if (daysInput.Count != 7)
        {
            failures.Add(new ValidationError(ToFieldName(nameof(Request.Days)), "Days must contain exactly 7 entries."));
        }

        var candidates = new List<PlannedDayCandidate>();
        var seenDates = new HashSet<DateOnly>();

        for (var index = 0; index < daysInput.Count; index++)
        {
            var day = daysInput[index];
            if (day is null)
            {
                failures.Add(new ValidationError(BuildDayField(index), "Day is required."));
                continue;
            }

            if (!TryParseDate(day.Date, out var date))
            {
                failures.Add(new ValidationError(
                    BuildDayField(index, nameof(PlannedDayInput.Date)),
                    "Day date is invalid."));
                continue;
            }

            if (!seenDates.Add(date))
            {
                failures.Add(new ValidationError(
                    BuildDayField(index, nameof(PlannedDayInput.Date)),
                    "Day date must be unique."));
            }

            if (day.Selection is null)
            {
                failures.Add(new ValidationError(
                    BuildDayField(index, nameof(PlannedDayInput.Selection)),
                    "Selection is required."));
                continue;
            }

            if (!TryParseSelectionType(day.Selection.Type, out var selectionType))
            {
                failures.Add(new ValidationError(
                    BuildDayField(index, nameof(DishSelectionInput.Type)),
                    "Selection type is invalid."));
                continue;
            }

            var dishId = ParseDishId(day.Selection.DishId, selectionType, index, failures);
            if (dishId is null && selectionType == DishSelectionType.Dish)
            {
                continue;
            }

            candidates.Add(new PlannedDayCandidate(date, selectionType, dishId, index));
        }

        if (startDate != DateOnly.MinValue)
        {
            ValidateDateRange(startDate, candidates, failures);
        }

        if (failures.Count > 0)
        {
            return new ValidationResult(false, null, failures);
        }

        var candidate = new WeeklyPlanCandidate(startDate, candidates);
        return new ValidationResult(true, candidate, Array.Empty<ValidationError>());
    }

    // Validates that all planned days cover the expected date range.
    private static void ValidateDateRange(
        DateOnly startDate,
        IReadOnlyList<PlannedDayCandidate> candidates,
        ICollection<ValidationError> failures)
    {
        var expectedDates = Enumerable.Range(0, 7)
            .Select(offset => startDate.AddDays(offset))
            .ToHashSet();

        foreach (var day in candidates)
        {
            if (!expectedDates.Contains(day.Date))
            {
                failures.Add(new ValidationError(
                    BuildDayField(day.Index, nameof(PlannedDayInput.Date)),
                    "Day date must be within the weekly plan range."));
            }
        }

        if (candidates.Count == 0)
        {
            return;
        }

        var candidateDates = candidates
            .Select(day => day.Date)
            .ToHashSet();

        if (expectedDates.Any(date => !candidateDates.Contains(date)))
        {
            failures.Add(new ValidationError(
                ToFieldName(nameof(Request.Days)),
                "Days must cover every date from startDate to startDate + 6."));
        }
    }

    // Parses the selection dish id and applies selection rules.
    private static Guid? ParseDishId(
        string? value,
        DishSelectionType selectionType,
        int index,
        ICollection<ValidationError> failures)
    {
        if (selectionType == DishSelectionType.Empty)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                failures.Add(new ValidationError(
                    BuildDayField(index, nameof(DishSelectionInput.DishId)),
                    "Dish id must be empty when selection is EMPTY."));
            }

            return null;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add(new ValidationError(
                BuildDayField(index, nameof(DishSelectionInput.DishId)),
                "Dish id is required when selection is DISH."));
            return null;
        }

        if (!Guid.TryParse(value, out var parsed) || parsed == Guid.Empty)
        {
            failures.Add(new ValidationError(
                BuildDayField(index, nameof(DishSelectionInput.DishId)),
                "Dish id is invalid."));
            return null;
        }

        return parsed;
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

    // Parses selection type values into the domain enum.
    private static bool TryParseSelectionType(string? value, out DishSelectionType type)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            type = default;
            return false;
        }

        return Enum.TryParse(value, true, out type);
    }

    // Builds the field name for a planned day input.
    internal static string BuildDayField(int index, string? property = null)
    {
        var prefix = $"{ToFieldName(nameof(Request.Days))}[{index}]";

        if (string.IsNullOrWhiteSpace(property))
        {
            return prefix;
        }

        return $"{prefix}.{ToFieldName(property)}";
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
    WeeklyPlanCandidate? Candidate,
    IReadOnlyList<ValidationError> Errors);

internal sealed record WeeklyPlanCandidate(
    DateOnly StartDate,
    IReadOnlyList<PlannedDayCandidate> Days);

internal sealed record PlannedDayCandidate(
    DateOnly Date,
    DishSelectionType SelectionType,
    Guid? DishId,
    int Index);
