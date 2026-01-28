using System.Text.Json.Serialization;

namespace Middagsklok.Api.Features.Dishes.Import;

public sealed record Request(
    [property: JsonPropertyName("dishes")] IReadOnlyList<DishInput>? Dishes);

public sealed record DishInput(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("activeMinutes")] int ActiveMinutes,
    [property: JsonPropertyName("totalMinutes")] int TotalMinutes,
    [property: JsonPropertyName("ingredients")] IReadOnlyList<IngredientInput>? Ingredients);

public sealed record IngredientInput(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("category")] string? Category,
    [property: JsonPropertyName("amount")] double Amount,
    [property: JsonPropertyName("unit")] string? Unit);
