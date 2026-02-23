namespace Middagsklok.Api.Features.Recipes.Instructions;

public sealed record Response(IReadOnlyList<RecipeInstruction> Recipes);

public sealed record RecipeInstruction(
    string DishId,
    string DishName,
    string? Summary,
    int? TotalMinutes,
    int? Servings,
    IReadOnlyList<RecipeInstructionStep> Steps);

public sealed record RecipeInstructionStep(
    int Order,
    string? Heading,
    string Description);
