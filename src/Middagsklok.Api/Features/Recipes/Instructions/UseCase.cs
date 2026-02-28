using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.Ingredient;
using DishEntity = Middagsklok.Api.Domain.Dish.Dish;
using DishIngredientEntity = Middagsklok.Api.Domain.Dish.DishIngredient;

namespace Middagsklok.Api.Features.Recipes.Instructions;

internal sealed class UseCase(AppDbContext dbContext)
{
    private readonly AppDbContext _dbContext = dbContext;

    // Executes the query for recipe instructions from persisted dishes.
    public async Task<Response> Execute(CancellationToken cancellationToken)
    {
        var dishes = await _dbContext.Dishes
            .AsNoTracking()
            .OrderBy(dish => dish.Name)
            .ToListAsync(cancellationToken);

        var ingredientIds = dishes
            .SelectMany(dish => dish.Ingredients)
            .Select(ingredient => ingredient.IngredientId)
            .Distinct()
            .ToList();

        var ingredients = await _dbContext.Ingredients
            .AsNoTracking()
            .Where(ingredient => ingredientIds.Contains(ingredient.Id))
            .ToDictionaryAsync(ingredient => ingredient.Id, cancellationToken);

        var recipes = dishes
            .Select(dish => MapRecipe(dish, ingredients))
            .ToArray();

        var response = new Response(recipes);

        return response;
    }

    // Maps one dish entity into a recipe instruction response.
    private static RecipeInstruction MapRecipe(DishEntity dish, Dictionary<Guid, Ingredient> ingredients)
    {
        var steps = BuildSteps(dish.Instructions);
        var summary = BuildSummary(dish.Instructions, steps);
        var recipeIngredients = MapIngredients(dish.Ingredients, ingredients);

        var recipe = new RecipeInstruction(
            dish.Id.ToString("D"),
            dish.Name,
            summary,
            dish.TotalTimeMinutes,
            dish.Servings,
            recipeIngredients,
            steps);

        return recipe;
    }

    // Maps dish ingredients to recipe ingredients with resolved names.
    private static IReadOnlyList<RecipeIngredient> MapIngredients(
        IReadOnlyList<DishIngredientEntity> dishIngredients,
        Dictionary<Guid, Ingredient> ingredients)
    {
        var recipeIngredients = dishIngredients
            .OrderBy(ingredient => ingredient.SortOrder ?? int.MaxValue)
            .ThenBy(ingredient => ingredients.TryGetValue(ingredient.IngredientId, out var ing) ? ing.Name : "")
            .Select(dishIngredient =>
            {
                var name = ingredients.TryGetValue(dishIngredient.IngredientId, out var ingredient)
                    ? ingredient.Name
                    : "Unknown ingredient";

                return new RecipeIngredient(
                    dishIngredient.IngredientId.ToString("D"),
                    name,
                    dishIngredient.Quantity,
                    dishIngredient.Unit.ToString(),
                    dishIngredient.Note);
            })
            .ToArray();

        return recipeIngredients;
    }

    // Builds normalized instruction steps from free-form instructions text.
    private static IReadOnlyList<RecipeInstructionStep> BuildSteps(string? instructions)
    {
        var lines = SplitLines(instructions);
        if (lines.Count == 0)
        {
            return [new RecipeInstructionStep(1, null, "No instructions provided yet.")];
        }

        var steps = lines
            .Select((line, index) =>
            {
                var cleanLine = StripListPrefix(line);
                return new RecipeInstructionStep(index + 1, null, cleanLine);
            })
            .ToArray();

        return steps;
    }

    // Splits raw instructions into step-like lines.
    private static IReadOnlyList<string> SplitLines(string? instructions)
    {
        var trimmed = instructions?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return Array.Empty<string>();
        }

        var byNewLine = trimmed
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        if (byNewLine.Length > 1)
        {
            return byNewLine;
        }

        var bySentence = trimmed
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(sentence => !string.IsNullOrWhiteSpace(sentence))
            .Select(sentence => sentence.EndsWith('.') ? sentence : $"{sentence}.")
            .ToArray();

        if (bySentence.Length > 1)
        {
            return bySentence;
        }

        return [trimmed];
    }

    // Removes common ordered-list prefixes from text.
    private static string StripListPrefix(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length < 3)
        {
            return trimmed;
        }

        if (!char.IsDigit(trimmed[0]))
        {
            return trimmed;
        }

        var separatorIndex = trimmed.IndexOfAny(['.', ')', '-']);
        if (separatorIndex <= 0)
        {
            return trimmed;
        }

        var candidate = trimmed[(separatorIndex + 1)..].TrimStart();
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return trimmed;
        }

        return candidate;
    }

    // Builds a concise summary from instructions.
    private static string? BuildSummary(string? instructions, IReadOnlyList<RecipeInstructionStep> steps)
    {
        var trimmed = instructions?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            return Truncate(trimmed, 180);
        }

        var firstStep = steps.FirstOrDefault();
        if (firstStep is null)
        {
            return null;
        }

        return Truncate(firstStep.Description, 180);
    }

    // Truncates text without splitting on invalid indices.
    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        var truncated = value[..maxLength].TrimEnd();

        return $"{truncated}...";
    }
}
