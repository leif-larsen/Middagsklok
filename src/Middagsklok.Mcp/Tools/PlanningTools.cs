using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using Middagsklok.Mcp.Planning;
using ModelContextProtocol.Server;

namespace Middagsklok.Mcp.Tools;

// The seven task-shaped tools Claude uses to plan a week and shop for it.
[McpServerToolType]
public sealed class PlanningTools
{
    [McpServerTool(Name = "get_weekly_plan"), Description(
        "Returns the dinner plan for a week with dish ids resolved to names. " +
        "Omit start_date to get the current week, using the household's configured first day of the week.")]
    public static async Task<string> GetWeeklyPlan(
        MiddagsklokApiClient client,
        [Description("Week start date as yyyy-MM-dd. Optional; defaults to the current week.")] string? startDate = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedStart = await ResolveStartDate(client, startDate, cancellationToken);
        var planJson = await client.GetWeeklyPlan(resolvedStart, cancellationToken);

        if (planJson is null)
        {
            return Message($"No plan exists for the week starting {resolvedStart}.", resolvedStart);
        }

        var dishNames = await LoadDishNames(client, cancellationToken);
        var plan = JsonNode.Parse(planJson)!.AsObject();

        foreach (var day in plan["days"]!.AsArray())
        {
            var selection = day!["selection"]!.AsObject();
            var dishId = selection["dishId"]?.GetValue<string>();
            selection["dishName"] = dishId is not null && dishNames.TryGetValue(dishId, out var name) ? name : null;
        }

        return plan.ToJsonString(JsonOptions);
    }

    [McpServerTool(Name = "list_dishes"), Description(
        "Lists the dish repertoire: names, ids, type, flags (seafood, vegetarian, vegan), vibe tags, " +
        "total time, ingredients and when each dish was last eaten. Retired dishes are excluded unless asked for.")]
    public static async Task<string> ListDishes(
        MiddagsklokApiClient client,
        [Description("Include retired dishes. Defaults to false.")] bool includeRetired = false,
        CancellationToken cancellationToken = default)
    {
        var dishesJson = await client.GetDishes(includeRetired, cancellationToken);
        var dishes = JsonNode.Parse(dishesJson)!["dishes"]!.AsArray();

        foreach (var dish in dishes)
        {
            var dishObject = dish!.AsObject();
            dishObject["totalMinutes"] = (dishObject["prepMinutes"]?.GetValue<int>() ?? 0)
                + (dishObject["cookMinutes"]?.GetValue<int>() ?? 0);
            dishObject.Remove("instructions");
        }

        return dishes.ToJsonString(JsonOptions);
    }

    [McpServerTool(Name = "generate_weekly_plan"), Description(
        "Lets Middagsklok generate a plan for the week. The API persists the result immediately, so this " +
        "refuses to run when the week already has planned days unless overwrite is true.")]
    public static async Task<string> GenerateWeeklyPlan(
        MiddagsklokApiClient client,
        [Description("Week start date as yyyy-MM-dd.")] string startDate,
        [Description("Set true to replace a week that already has planned days.")] bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        var existing = await client.GetWeeklyPlan(startDate, cancellationToken);

        if (existing is not null && !overwrite && OverwriteGuard.HasPlannedDays(existing))
        {
            var refusal = new JsonObject
            {
                ["warning"] = $"The week starting {startDate} already has planned days. Nothing was generated. " +
                    "Call again with overwrite = true to replace it, or use set_weekly_plan for targeted changes.",
                ["existingPlan"] = JsonNode.Parse(existing)
            };

            return refusal.ToJsonString(JsonOptions);
        }

        var generated = await client.GenerateWeeklyPlan(startDate, cancellationToken);

        return generated;
    }

    [McpServerTool(Name = "set_weekly_plan"), Description(
        "Applies targeted edits to a week: set a dish on a day, clear a day, or override servings for one meal " +
        "(guests). Days not listed keep their current value. Dish ids come from list_dishes.")]
    public static async Task<string> SetWeeklyPlan(
        MiddagsklokApiClient client,
        [Description("Week start date as yyyy-MM-dd.")] string startDate,
        [Description("Days to change. Each has date (yyyy-MM-dd), optional dishId (null clears the day) and optional servings.")]
        IReadOnlyList<PlannedDayEdit> days,
        CancellationToken cancellationToken = default)
    {
        if (!DateOnly.TryParseExact(startDate, "yyyy-MM-dd", out var weekStart))
        {
            return Message("startDate must be yyyy-MM-dd.", startDate);
        }

        var existing = await client.GetWeeklyPlan(startDate, cancellationToken);
        var body = PlanEditor.BuildUpsertBody(weekStart, existing, days);
        var updated = await client.UpsertWeeklyPlan(startDate, body, cancellationToken);

        return updated;
    }

    [McpServerTool(Name = "get_shopping_list"), Description(
        "Returns the shopping list for a week with amounts scaled to the planned servings. Each item carries " +
        "odaStatus (Unmapped, Mapped, NotAvailable) and, when mapped, the Oda product and suggested package count. " +
        "Pantry staples are flagged and normally skipped.")]
    public static async Task<string> GetShoppingList(
        MiddagsklokApiClient client,
        [Description("Week start date as yyyy-MM-dd.")] string startDate,
        CancellationToken cancellationToken = default)
    {
        var list = await client.GetShoppingList(startDate, cancellationToken);

        return list ?? Message($"No shopping list for the week starting {startDate}: no plan exists or it is already marked eaten.", startDate);
    }

    [McpServerTool(Name = "set_ingredient_oda_mapping"), Description(
        "Records which Oda product an ingredient resolves to, so future shopping lists skip the search. " +
        "Call only after the person has confirmed the product. Pass notAvailable = true for things Oda does not stock.")]
    public static async Task<string> SetIngredientOdaMapping(
        MiddagsklokApiClient client,
        [Description("Ingredient id from the shopping list.")] string ingredientId,
        [Description("Oda product id. Omit when notAvailable is true.")] int? productId = null,
        [Description("Oda product name as shown in the shop, for display and drift detection.")] string? productName = null,
        [Description("Size of one retail package, e.g. 400.")] double? packQuantity = null,
        [Description("Unit of the package size: G, Kg, Ml, L or Pcs.")] string? packUnit = null,
        [Description("True when Oda does not stock this ingredient.")] bool notAvailable = false,
        CancellationToken cancellationToken = default)
    {
        var request = notAvailable
            ? new JsonObject { ["notAvailable"] = true }
            : new JsonObject
            {
                ["productId"] = productId,
                ["productName"] = productName,
                ["packQuantity"] = packQuantity,
                ["packUnit"] = packUnit,
                ["confirmed"] = true
            };

        var mapping = await client.SetOdaMapping(ingredientId, request, cancellationToken);

        return mapping;
    }

    [McpServerTool(Name = "mark_week_eaten"), Description(
        "Marks the week as eaten so its dishes count in the days-between rotation logic. " +
        "Do this once the week is over, not when the plan is made.")]
    public static async Task<string> MarkWeekEaten(
        MiddagsklokApiClient client,
        [Description("Week start date as yyyy-MM-dd.")] string startDate,
        CancellationToken cancellationToken = default)
    {
        await client.MarkWeekEaten(startDate, cancellationToken);

        return Message($"Week starting {startDate} marked as eaten.", startDate);
    }

    // Resolves the requested start date, or the current week from the household settings.
    private static async Task<string> ResolveStartDate(MiddagsklokApiClient client, string? startDate, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(startDate))
        {
            return startDate.Trim();
        }

        var settingsJson = await client.GetPlanningSettings(cancellationToken);
        using var settings = JsonDocument.Parse(settingsJson);
        var weekStartsOn = WeekResolver.ParseWeekStartsOn(settings.RootElement.GetProperty("weekStartsOn").GetString());
        var today = DateOnly.FromDateTime(DateTime.Now);
        var resolved = WeekResolver.Format(WeekResolver.WeekStartFor(today, weekStartsOn));

        return resolved;
    }

    // Loads a dish id to name lookup across active and retired dishes, so old plans still resolve.
    private static async Task<IReadOnlyDictionary<string, string>> LoadDishNames(MiddagsklokApiClient client, CancellationToken cancellationToken)
    {
        var dishesJson = await client.GetDishes(true, cancellationToken);
        using var document = JsonDocument.Parse(dishesJson);
        var names = document.RootElement
            .GetProperty("dishes")
            .EnumerateArray()
            .ToDictionary(
                dish => dish.GetProperty("id").GetString()!,
                dish => dish.GetProperty("name").GetString()!);

        return names;
    }

    // Wraps a human-readable outcome in JSON so every tool answers in the same shape.
    private static string Message(string message, string startDate) =>
        new JsonObject { ["message"] = message, ["startDate"] = startDate }.ToJsonString(JsonOptions);

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };
}
