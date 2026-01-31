using System.Text.Json.Serialization;

namespace Middagsklok.Api.Features.Settings.Upsert;

public sealed record Request(
    [property: JsonPropertyName("weekStartsOn")] string? WeekStartsOn,
    [property: JsonPropertyName("seafoodPerWeek")] int? SeafoodPerWeek);
