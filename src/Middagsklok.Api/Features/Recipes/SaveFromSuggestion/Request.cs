using System.Text.Json.Serialization;

namespace Middagsklok.Api.Features.Recipes.SaveFromSuggestion;

public sealed record Request(
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("summary")] string? Summary,
    [property: JsonPropertyName("estimatedTotalMinutes")] int? EstimatedTotalMinutes);
