using System.Globalization;

namespace Middagsklok.Mcp.Planning;

// Resolves which week a date belongs to, given the household's configured first day of the week.
public static class WeekResolver
{
    // Returns the most recent start-of-week on or before the given date.
    public static DateOnly WeekStartFor(DateOnly date, DayOfWeek weekStartsOn)
    {
        var offset = ((int)date.DayOfWeek - (int)weekStartsOn + 7) % 7;
        var weekStart = date.AddDays(-offset);

        return weekStart;
    }

    // Parses a week-start setting such as "Friday" into a DayOfWeek, defaulting to Monday.
    public static DayOfWeek ParseWeekStartsOn(string? value) =>
        Enum.TryParse<DayOfWeek>(value, true, out var parsed) ? parsed : DayOfWeek.Monday;

    // Formats a date the way the API expects it in routes.
    public static string Format(DateOnly date) =>
        date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
