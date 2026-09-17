using System.Text.Json;

namespace Middagsklok.Mcp.Planning;

// Decides whether generating a plan would destroy something a person already planned.
public static class OverwriteGuard
{
    // Returns true when the plan JSON has at least one day with a dish selected.
    public static bool HasPlannedDays(string planJson)
    {
        using var document = JsonDocument.Parse(planJson);

        if (!document.RootElement.TryGetProperty("days", out var days) || days.ValueKind is not JsonValueKind.Array)
        {
            return false;
        }

        var hasPlannedDay = days
            .EnumerateArray()
            .Any(day => day.TryGetProperty("selection", out var selection)
                && selection.TryGetProperty("type", out var type)
                && !string.Equals(type.GetString(), "EMPTY", StringComparison.OrdinalIgnoreCase));

        return hasPlannedDay;
    }
}
