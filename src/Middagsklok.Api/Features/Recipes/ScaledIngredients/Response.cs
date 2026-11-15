namespace Middagsklok.Api.Features.Recipes.ScaledIngredients;

public sealed record Response(
    string DishId,
    string DishName,
    int Serves,
    int Servings,
    IReadOnlyList<ScaledIngredient> Ingredients);

// One ingredient resolved to the amount needed for the requested servings.
// Amount and Unit are the raw scaled value; DisplayAmount and DisplayUnit are a kitchen-friendly rendering of it.
public sealed record ScaledIngredient(
    string IngredientId,
    string Name,
    double Amount,
    string Unit,
    string Scaling,
    string DisplayAmount,
    string DisplayUnit,
    string? Note);

public sealed record ErrorResponse(
    string Message,
    IReadOnlyList<ValidationError> Errors);

public sealed record ValidationError(string Field, string Message);
