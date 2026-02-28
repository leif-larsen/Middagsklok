namespace Middagsklok.Api.Features.Recipes.SaveFromSuggestion;

public sealed record Response(
    string Id,
    string Name,
    string DishType,
    int PrepTimeMinutes,
    int CookTimeMinutes,
    int Servings,
    string? Instructions,
    bool IsSeafood,
    bool IsVegetarian,
    bool IsVegan,
    IReadOnlyList<string> VibeTags,
    IReadOnlyList<DishIngredientResponse> Ingredients);

public sealed record DishIngredientResponse(
    string Id,
    string IngredientId,
    double Quantity,
    string Label);

public sealed record ErrorResponse(
    string Message,
    IReadOnlyList<ValidationError> Errors);

public sealed record ValidationError(string Field, string Message);
