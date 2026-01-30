namespace Middagsklok.Api.Features.Dishes.Update;

public sealed record Response(
    string Id,
    string Name,
    string Cuisine,
    int PrepMinutes,
    int CookMinutes,
    int Serves,
    string? Instructions,
    IEnumerable<DishIngredientResponse> Ingredients);

public sealed record DishIngredientResponse(
    string Id,
    string Label);

public sealed record ErrorResponse(
    string Message,
    IReadOnlyList<ValidationError> Errors);

public sealed record ValidationError(string Field, string Message);
