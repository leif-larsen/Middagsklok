using System.Text.Json.Serialization;

namespace Middagsklok.Api.Features.Recipes.Suggestions;

public sealed record Request(
    [property: JsonPropertyName("prompt")] string? Prompt,
    [property: JsonPropertyName("maxSuggestions")] int? MaxSuggestions);
