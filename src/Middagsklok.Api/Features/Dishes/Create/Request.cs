using System.Text.Json.Serialization;

namespace Middagsklok.Api.Features.Dishes.Create;

public sealed record Request(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("cuisine")] string? Cuisine,
    [property: JsonPropertyName("prepMinutes")] int PrepMinutes,
    [property: JsonPropertyName("cookMinutes")] int CookMinutes,
    [property: JsonPropertyName("serves")] int Serves,
    [property: JsonPropertyName("instructions")] string? Instructions,
    [property: JsonPropertyName("isSeafood")] bool IsSeafood,
    [property: JsonPropertyName("isVegetarian")] bool IsVegetarian,
    [property: JsonPropertyName("isVegan")] bool IsVegan,
    [property: JsonPropertyName("vibeTags")] IReadOnlyList<string>? VibeTags,
    [property: JsonPropertyName("ingredients")] IReadOnlyList<IngredientInput>? Ingredients);

public sealed record IngredientInput(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("amount")] double Amount);
