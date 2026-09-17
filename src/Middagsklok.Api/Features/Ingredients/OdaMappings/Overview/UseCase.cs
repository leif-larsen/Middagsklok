using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.Ingredient;

namespace Middagsklok.Api.Features.Ingredients.OdaMappings.Overview;

internal sealed class UseCase(AppDbContext dbContext)
{
    private readonly AppDbContext _dbContext = dbContext;

    // Executes the query for all Oda mappings plus the ingredients still unmapped.
    public async Task<Response> Execute(CancellationToken cancellationToken)
    {
        var ingredients = await _dbContext.Ingredients
            .AsNoTracking()
            .OrderBy(ingredient => ingredient.Name)
            .ToListAsync(cancellationToken);

        var usageLookup = await LoadUsageCounts(cancellationToken);

        var mappings = ingredients
            .Where(ingredient => ingredient.OdaMapping is not null)
            .Select(ingredient => MapMapped(ingredient, usageLookup))
            .ToArray();

        // Unmapped ingredients are ordered by usage so the most valuable ones come first.
        var unmapped = ingredients
            .Where(ingredient => ingredient.OdaMapping is null)
            .Select(ingredient => MapUnmapped(ingredient, usageLookup))
            .OrderByDescending(ingredient => ingredient.UsedIn)
            .ThenBy(ingredient => ingredient.IngredientName)
            .ToArray();

        var response = new Response(mappings, unmapped);

        return response;
    }

    // Loads how many dishes use each ingredient.
    private async Task<IReadOnlyDictionary<Guid, int>> LoadUsageCounts(CancellationToken cancellationToken)
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

    // Maps a mapped ingredient to its response row.
    private static MappedIngredient MapMapped(Ingredient ingredient, IReadOnlyDictionary<Guid, int> usageLookup)
    {
        var mapping = ingredient.OdaMapping!;
        var row = new MappedIngredient(
            ingredient.Id.ToString("D"),
            ingredient.Name,
            mapping.Availability.ToString(),
            mapping.OdaProductId,
            mapping.OdaProductName,
            mapping.PackQuantity,
            mapping.PackUnit?.ToString(),
            mapping.IsConfirmed,
            UsedIn(ingredient, usageLookup));

        return row;
    }

    // Maps an unmapped ingredient to its response row.
    private static UnmappedIngredient MapUnmapped(Ingredient ingredient, IReadOnlyDictionary<Guid, int> usageLookup) =>
        new(
            ingredient.Id.ToString("D"),
            ingredient.Name,
            ingredient.Category.ToString(),
            ingredient.DefaultUnit.ToString(),
            ingredient.IsPantryStaple,
            UsedIn(ingredient, usageLookup));

    // Resolves the dish count for an ingredient.
    private static int UsedIn(Ingredient ingredient, IReadOnlyDictionary<Guid, int> usageLookup) =>
        usageLookup.TryGetValue(ingredient.Id, out var count) ? count : 0;
}
