using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.Dish;
using Middagsklok.Api.Domain.Ingredient;

namespace Middagsklok.Api.Features.Dishes.Overview;

internal sealed class UseCase(AppDbContext dbContext)
{
    private readonly AppDbContext _dbContext = dbContext;

    // Executes the query for all dishes.
    public async Task<Response> Execute(CancellationToken cancellationToken)
    {
        var dishes = await _dbContext.Dishes
            .AsNoTracking()
            .Include(d => d.Ingredients)
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);

        if (dishes.Count == 0)
        {
            var emptyResponse = new Response(Array.Empty<DishOverview>());
            return emptyResponse;
        }

        var ingredientIds = dishes
            .SelectMany(d => d.Ingredients)
            .Select(i => i.IngredientId)
            .Distinct()
            .ToArray();

        var ingredientLookup = await LoadIngredients(ingredientIds, cancellationToken);

        var dishOverviews = dishes
            .Select(dish => MapDish(dish, ingredientLookup))
            .ToArray();

        var response = new Response(dishOverviews);

        return response;
    }

    // Loads ingredients by id for label composition.
    private async Task<IReadOnlyDictionary<Guid, Ingredient>> LoadIngredients(
        IReadOnlyList<Guid> ingredientIds,
        CancellationToken cancellationToken)
    {
        if (ingredientIds.Count == 0)
        {
            return new Dictionary<Guid, Ingredient>();
        }

        var ingredients = await _dbContext.Ingredients
            .AsNoTracking()
            .Where(i => ingredientIds.Contains(i.Id))
            .ToListAsync(cancellationToken);

        var lookup = ingredients.ToDictionary(i => i.Id);

        return lookup;
    }

    // Maps a dish entity to the overview response.
    private static DishOverview MapDish(
        Dish dish,
        IReadOnlyDictionary<Guid, Ingredient> ingredientLookup)
    {
        var ingredients = dish.Ingredients
            .OrderBy(i => i.SortOrder ?? int.MaxValue)
            .Select((ingredient, index) =>
            {
                var ingredientName = ingredientLookup.TryGetValue(ingredient.IngredientId, out var ingredientEntity)
                    ? ingredientEntity.Name
                    : string.Empty;

                var ingredientId = ingredient.IngredientId;
                var label = BuildIngredientLabel(ingredient.Quantity, ingredient.Unit, ingredientName);
                var id = $"{ingredientId:D}-{index + 1}";

                return new DishIngredientOverview(
                    id,
                    ingredientId.ToString("D"),
                    ingredient.Quantity,
                    label);
            })
            .ToArray();

        var overview = new DishOverview(
            dish.Id.ToString("D"),
            dish.Name,
            dish.Cuisine.ToString(),
            dish.PrepTimeMinutes,
            dish.CookTimeMinutes,
            dish.Servings,
            dish.Instructions,
            ingredients);

        return overview;
    }

    // Builds a human-readable ingredient label.
    private static string BuildIngredientLabel(double quantity, Unit unit, string name)
    {
        var quantityLabel = FormatQuantity(quantity);
        var unitLabel = FormatUnit(unit);
        var trimmedName = name?.Trim() ?? string.Empty;

        var label = string.Join(
            " ",
            new[] { quantityLabel, unitLabel, trimmedName }
                .Where(value => !string.IsNullOrWhiteSpace(value)));

        return label;
    }

    // Formats ingredient quantities for display.
    private static string FormatQuantity(double quantity)
    {
        if (quantity <= 0)
        {
            return string.Empty;
        }

        var hasFraction = Math.Abs(quantity % 1) > 0.0001;
        var format = hasFraction ? "0.##" : "0";
        var formatted = quantity.ToString(format, CultureInfo.InvariantCulture);

        return formatted;
    }

    // Formats unit values for labels.
    private static string FormatUnit(Unit unit) =>
        unit switch
        {
            Unit.G => "g",
            Unit.Kg => "kg",
            Unit.Ml => "ml",
            Unit.L => "l",
            Unit.Pcs => "pcs",
            _ => string.Empty
        };
}
