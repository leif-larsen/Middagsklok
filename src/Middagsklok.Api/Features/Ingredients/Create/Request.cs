using System.Text.Json.Serialization;

namespace Middagsklok.Api.Features.Ingredients.Create;

public sealed record Request(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("category")] string? Category,
    [property: JsonPropertyName("defaultUnit")] string? DefaultUnit);
