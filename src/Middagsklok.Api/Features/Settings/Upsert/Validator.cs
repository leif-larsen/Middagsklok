namespace Middagsklok.Api.Features.Settings.Upsert;

internal sealed class Validator
{
    // Validates the planning settings request and returns a candidate with any failures.
    public ValidationResult Validate(Request? request)
    {
        var failures = new List<ValidationError>();

        if (request is null)
        {
            failures.Add(new ValidationError(string.Empty, "Planning settings are required."));
            return new ValidationResult(false, null, failures);
        }

        if (!TryParseWeekStartsOn(request.WeekStartsOn, out var weekStartsOn))
        {
            failures.Add(new ValidationError(
                ToFieldName(nameof(Request.WeekStartsOn)),
                "Week start day is invalid."));
        }

        var seafoodPerWeek = request.SeafoodPerWeek.GetValueOrDefault();
        var daysBetween = request.DaysBetween.GetValueOrDefault();

        if (request.SeafoodPerWeek is null)
        {
            failures.Add(new ValidationError(
                ToFieldName(nameof(Request.SeafoodPerWeek)),
                "Seafood per week is required."));
        }
        else if (seafoodPerWeek < 0 || seafoodPerWeek > 7)
        {
            failures.Add(new ValidationError(
                ToFieldName(nameof(Request.SeafoodPerWeek)),
                "Seafood per week must be between 0 and 7."));
        }

        if (request.DaysBetween is null)
        {
            failures.Add(new ValidationError(
                ToFieldName(nameof(Request.DaysBetween)),
                "Days between is required."));
        }
        else if (daysBetween < 0 || daysBetween > 30)
        {
            failures.Add(new ValidationError(
                ToFieldName(nameof(Request.DaysBetween)),
                "Days between must be between 0 and 30."));
        }

        if (failures.Count > 0)
        {
            return new ValidationResult(false, null, failures);
        }

        var candidate = new PlanningSettingsCandidate(weekStartsOn, seafoodPerWeek, daysBetween);
        return new ValidationResult(true, candidate, Array.Empty<ValidationError>());
    }

    // Parses the week start day value into the DayOfWeek enum.
    private static bool TryParseWeekStartsOn(string? value, out DayOfWeek weekStartsOn)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            weekStartsOn = default;
            return false;
        }

        return Enum.TryParse(value, true, out weekStartsOn);
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
    PlanningSettingsCandidate? Candidate,
    IReadOnlyList<ValidationError> Errors);

internal sealed record PlanningSettingsCandidate(
    DayOfWeek WeekStartsOn,
    int SeafoodPerWeek,
    int DaysBetween);
