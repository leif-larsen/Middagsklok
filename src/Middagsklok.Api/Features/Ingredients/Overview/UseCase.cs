using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.Ingredient;

namespace Middagsklok.Api.Features.Ingredients.Overview;

internal sealed class UseCase(AppDbContext dbContext)
{
    private readonly AppDbContext _dbContext = dbContext;

    // Executes the query for all ingredients.
    public async Task<Response> Execute(CancellationToken cancellationToken)
    {
        var ingredients = await _dbContext.Ingredients
            .AsNoTracking()
            .OrderBy(ingredient => ingredient.Name)
            .ToListAsync(cancellationToken);

        if (ingredients.Count == 0)
        {
            var emptyResponse = new Response(Array.Empty<IngredientOverview>());
            return emptyResponse;
        }

        var usageLookup = await LoadUsageCounts(cancellationToken);

        var overviews = ingredients
            .Select(ingredient => MapIngredient(ingredient, usageLookup))
            .ToArray();

        var response = new Response(overviews);

        return response;
    }

    // Loads how many dishes use each ingredient.
    private async Task<IReadOnlyDictionary<Guid, int>> LoadUsageCounts(
        CancellationToken cancellationToken)
    {
        var usage = await _dbContext.Dishes
            .AsNoTracking()
            .SelectMany(dish => dish.Ingredients.Select(ingredient => new
            {
                DishId = dish.Id,
                ingredient.IngredientId
            }))
            .GroupBy(entry => entry.IngredientId)
            .Select(group => new
            {
                group.Key,
                Count = group.Select(entry => entry.DishId).Distinct().Count()
            })
            .ToListAsync(cancellationToken);

        var lookup = usage.ToDictionary(entry => entry.Key, entry => entry.Count);

        return lookup;
    }

    // Maps an ingredient entity to the overview response.
    private static IngredientOverview MapIngredient(
        Ingredient ingredient,
        IReadOnlyDictionary<Guid, int> usageLookup)
    {
        var usedIn = usageLookup.TryGetValue(ingredient.Id, out var count) ? count : 0;
        var category = FormatCategory(ingredient.Category);
        var defaultUnit = FormatUnit(ingredient.DefaultUnit);

        var overview = new IngredientOverview(
            ingredient.Id.ToString("D"),
            ingredient.Name,
            category,
            defaultUnit,
            usedIn);

        return overview;
    }

    // Formats category values for API responses.
    private static string FormatCategory(IngredientCategory category) =>
        category.ToString();

    // Formats unit values for API responses.
    private static string FormatUnit(Unit unit) =>
        unit.ToString();
}
