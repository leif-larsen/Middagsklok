using System.Text.Json;
using System.Text.Json.Nodes;

namespace Middagsklok.Mcp.Planning;

// Merges targeted day edits into a full seven-day plan, which is what the API's upsert expects.
public static class PlanEditor
{
    // Builds the seven-day upsert body from the existing plan (if any) with the given edits applied.
    public static JsonObject BuildUpsertBody(
        DateOnly weekStart,
        string? existingPlanJson,
        IReadOnlyList<PlannedDayEdit> edits)
    {
        var days = new Dictionary<string, JsonObject>();

        for (var offset = 0; offset < 7; offset++)
        {
            var date = WeekResolver.Format(weekStart.AddDays(offset));
            days[date] = EmptyDay(date);
        }

        if (existingPlanJson is not null)
        {
            using var document = JsonDocument.Parse(existingPlanJson);

            if (document.RootElement.TryGetProperty("days", out var existingDays)
                && existingDays.ValueKind is JsonValueKind.Array)
            {
                foreach (var day in existingDays.EnumerateArray())
                {
                    var date = day.GetProperty("date").GetString();

                    if (date is null || !days.ContainsKey(date))
                    {
                        continue;
                    }

                    var selection = day.GetProperty("selection");
                    var dishId = selection.TryGetProperty("dishId", out var dishIdElement)
                        ? dishIdElement.GetString()
                        : null;
                    int? servings = day.TryGetProperty("servings", out var servingsElement)
                        && servingsElement.ValueKind is JsonValueKind.Number
                        ? servingsElement.GetInt32()
                        : null;

                    days[date] = Day(date, dishId, servings);
                }
            }
        }

        foreach (var edit in edits)
        {
            if (!days.ContainsKey(edit.Date))
            {
                throw new PlanEditOutsideWeek(
                    $"Date {edit.Date} is not in the week starting {WeekResolver.Format(weekStart)}.");
            }

            days[edit.Date] = Day(edit.Date, edit.DishId, edit.Servings);
        }

        var body = new JsonObject
        {
            ["days"] = new JsonArray(days.Values.Select(day => (JsonNode)day).ToArray())
        };

        return body;
    }

    // Builds an empty day entry.
    private static JsonObject EmptyDay(string date) => Day(date, null, null);

    // Builds a day entry in the API's upsert shape.
    private static JsonObject Day(string date, string? dishId, int? servings)
    {
        var hasDish = !string.IsNullOrWhiteSpace(dishId);
        var day = new JsonObject
        {
            ["date"] = date,
            ["selection"] = new JsonObject
            {
                ["type"] = hasDish ? "DISH" : "EMPTY",
                ["dishId"] = hasDish ? dishId : null
            },
            ["servings"] = servings
        };

        return day;
    }
}

// One targeted change to a planned day. A null dish id clears the day.
public sealed record PlannedDayEdit(string Date, string? DishId, int? Servings);

/// <summary>
/// The exception that is thrown when an edit names a date outside the week being edited.
/// </summary>
public sealed class PlanEditOutsideWeek(string message) : Exception(message);
