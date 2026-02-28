namespace Middagsklok.Api.Features.Recipes.Instructions;

public sealed record Response(IReadOnlyList<RecipeInstruction> Recipes);

public sealed record RecipeInstruction(
    string DishId,
    string DishName,
    string? Summary,
    int? TotalMinutes,
    int? Servings,
    IReadOnlyList<RecipeIngredient> Ingredients,
    IReadOnlyList<RecipeInstructionStep> Steps);

public sealed record RecipeIngredient(
    string IngredientId,
    string Name,
    double Quantity,
    string Unit,
    string? Note);

public sealed record RecipeInstructionStep(
    int Order,
    string? Heading,
    string Description);
