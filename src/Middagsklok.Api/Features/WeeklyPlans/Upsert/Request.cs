using System.Text.Json.Serialization;

namespace Middagsklok.Api.Features.WeeklyPlans.Upsert;

public sealed record Request(
    [property: JsonPropertyName("days")] IReadOnlyList<PlannedDayInput>? Days);

public sealed record PlannedDayInput(
    [property: JsonPropertyName("date")] string? Date,
    [property: JsonPropertyName("selection")] DishSelectionInput? Selection);

public sealed record DishSelectionInput(
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("dishId")] string? DishId);
