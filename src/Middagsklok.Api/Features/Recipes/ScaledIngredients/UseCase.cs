using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.Dish;
using Middagsklok.Api.Domain.Ingredient;
using Middagsklok.Api.Domain.Settings;

namespace Middagsklok.Api.Features.Recipes.ScaledIngredients;

internal sealed class UseCase(AppDbContext dbContext)
{
    private readonly AppDbContext _dbContext = dbContext;

    // Resolves a dish's ingredients to the amounts needed for the given servings,
    // falling back to the household size when no servings count is supplied.
    public async Task<UseCaseResult> Execute(string? dishId, int? servings, CancellationToken cancellationToken)
    {
        var validator = new Validator();
        var validation = validator.Validate(dishId, servings);

        if (!validation.IsValid)
        {
            return new UseCaseResult(FetchOutcome.Invalid, null, validation.Errors);
        }

        var dish = await _dbContext.Dishes
            .AsNoTracking()
            .Include(existing => existing.Ingredients)
            .FirstOrDefaultAsync(existing => existing.Id == validation.DishId, cancellationToken);

        if (dish is null)
        {
            return new UseCaseResult(FetchOutcome.NotFound, null, Array.Empty<ValidationError>());
        }

        var resolvedServings = servings ?? await LoadDefaultServings(cancellationToken);
        var ingredients = await LoadIngredients(dish, cancellationToken);
        var scaledIngredients = MapIngredients(dish.Ingredients, ingredients, resolvedServings);

        var response = new Response(
            dish.Id.ToString("D"),
            dish.Name,
            dish.Servings,
            resolvedServings,
            scaledIngredients);

        var result = new UseCaseResult(FetchOutcome.Success, response, Array.Empty<ValidationError>());

        return result;
    }

    // Loads the household size, the same default the shopping list uses for days without an override.
    private async Task<int> LoadDefaultServings(CancellationToken cancellationToken)
    {
        var settings = await _dbContext.PlanningSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        return settings?.HouseholdSize ?? PlanningSettings.DefaultHouseholdSize;
    }

    // Loads the ingredient entities referenced by the dish.
    private async Task<Dictionary<Guid, Ingredient>> LoadIngredients(Dish dish, CancellationToken cancellationToken)
    {
        var ingredientIds = dish.Ingredients
            .Select(ingredient => ingredient.IngredientId)
            .Distinct()
            .ToArray();

        var ingredients = await _dbContext.Ingredients
            .AsNoTracking()
            .Where(ingredient => ingredientIds.Contains(ingredient.Id))
            .ToDictionaryAsync(ingredient => ingredient.Id, cancellationToken);

        return ingredients;
    }

    // Scales each dish ingredient with the domain rule shared with the shopping list, then formats it for display.
    private static IReadOnlyList<ScaledIngredient> MapIngredients(
        IReadOnlyList<DishIngredient> dishIngredients,
        Dictionary<Guid, Ingredient> ingredients,
        int servings)
    {
        var scaledIngredients = dishIngredients
            .OrderBy(ingredient => ingredient.SortOrder ?? int.MaxValue)
            .ThenBy(ingredient => ingredients.TryGetValue(ingredient.IngredientId, out var known) ? known.Name : "")
            .Select(dishIngredient =>
            {
                var name = ingredients.TryGetValue(dishIngredient.IngredientId, out var ingredient)
                    ? ingredient.Name
                    : "Unknown ingredient";

                var amount = Math.Round(dishIngredient.AmountFor(servings), 4, MidpointRounding.AwayFromZero);
                var display = AmountFormatter.Format(amount, dishIngredient.Unit);

                return new ScaledIngredient(
                    dishIngredient.IngredientId.ToString("D"),
                    name,
                    amount,
                    dishIngredient.Unit.ToString(),
                    dishIngredient.Scaling.ToString(),
                    display.Amount,
                    display.Unit,
                    dishIngredient.Note);
            })
            .ToArray();

        return scaledIngredients;
    }
}

internal enum FetchOutcome
{
    Success,
    NotFound,
    Invalid
}

internal sealed record UseCaseResult(
    FetchOutcome Outcome,
    Response? Recipe,
    IReadOnlyList<ValidationError> Errors);
