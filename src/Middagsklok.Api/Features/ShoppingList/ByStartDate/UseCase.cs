using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.Dish;
using Middagsklok.Api.Domain.Ingredient;
using Middagsklok.Api.Domain.WeeklyPlan;

namespace Middagsklok.Api.Features.ShoppingList.ByStartDate;

internal sealed class UseCase(AppDbContext dbContext)
{
    private readonly AppDbContext _dbContext = dbContext;

    // Executes the shopping list generation workflow.
    public async Task<UseCaseResult> Execute(string? startDate, CancellationToken cancellationToken)
    {
        var validator = new Validator();
        var validation = validator.Validate(startDate);

        if (!validation.IsValid)
        {
            var invalidResult = new UseCaseResult(FetchOutcome.Invalid, null, validation.Errors);
            return invalidResult;
        }

        var plan = await _dbContext.WeeklyPlans
            .AsNoTracking()
            .Include(existing => existing.Days)
            .FirstOrDefaultAsync(
                existing => existing.StartDate == validation.StartDate,
                cancellationToken);

        if (plan is null)
        {
            var notFoundResult = new UseCaseResult(
                FetchOutcome.NotFound,
                null,
                Array.Empty<ValidationError>());
            return notFoundResult;
        }

        var dishIds = plan.PlannedDishes
            .Select(day => day.Selection.DishId)
            .Where(dishId => dishId is not null)
            .Select(dishId => dishId!.Value)
            .Distinct()
            .ToArray();

        if (dishIds.Length == 0)
        {
            var emptyResponse = new Response(FormatDate(plan.StartDate), Array.Empty<ShoppingCategory>());
            var emptyResult = new UseCaseResult(
                FetchOutcome.Success,
                emptyResponse,
                Array.Empty<ValidationError>());
            return emptyResult;
        }

        var dishes = await LoadDishes(dishIds, cancellationToken);

        if (dishes.Count == 0)
        {
            var emptyResponse = new Response(FormatDate(plan.StartDate), Array.Empty<ShoppingCategory>());
            var emptyResult = new UseCaseResult(
                FetchOutcome.Success,
                emptyResponse,
                Array.Empty<ValidationError>());
            return emptyResult;
        }

        var ingredientLookup = await LoadIngredients(dishes, cancellationToken);
        var categories = BuildCategories(dishes, ingredientLookup);
        var response = new Response(FormatDate(plan.StartDate), categories);
        var result = new UseCaseResult(FetchOutcome.Success, response, Array.Empty<ValidationError>());

        return result;
    }

    // Loads the planned dishes for the weekly plan.
    private async Task<IReadOnlyList<Dish>> LoadDishes(
        IReadOnlyList<Guid> dishIds,
        CancellationToken cancellationToken)
    {
        var dishes = await _dbContext.Dishes
            .AsNoTracking()
            .Include(dish => dish.Ingredients)
            .Where(dish => dishIds.Contains(dish.Id))
            .ToListAsync(cancellationToken);

        return dishes;
    }

    // Loads ingredient entities for the planned dishes.
    private async Task<IReadOnlyDictionary<Guid, Ingredient>> LoadIngredients(
        IReadOnlyList<Dish> dishes,
        CancellationToken cancellationToken)
    {
        var ingredientIds = dishes
            .SelectMany(dish => dish.Ingredients)
            .Select(ingredient => ingredient.IngredientId)
            .Distinct()
            .ToArray();

        if (ingredientIds.Length == 0)
        {
            return new Dictionary<Guid, Ingredient>();
        }

        var ingredients = await _dbContext.Ingredients
            .AsNoTracking()
            .Where(ingredient => ingredientIds.Contains(ingredient.Id))
            .ToListAsync(cancellationToken);

        var lookup = ingredients.ToDictionary(ingredient => ingredient.Id);

        return lookup;
    }

    // Builds shopping list categories from dish ingredients.
    private static IEnumerable<ShoppingCategory> BuildCategories(
        IReadOnlyList<Dish> dishes,
        IReadOnlyDictionary<Guid, Ingredient> ingredientLookup)
    {
        var items = new Dictionary<ShoppingKey, ShoppingAggregate>();

        foreach (var dish in dishes)
        {
            foreach (var dishIngredient in dish.Ingredients)
            {
                if (!ingredientLookup.TryGetValue(dishIngredient.IngredientId, out var ingredient))
                {
                    continue;
                }

                var key = new ShoppingKey(ingredient.Id, dishIngredient.Unit);

                if (!items.TryGetValue(key, out var aggregate))
                {
                    aggregate = new ShoppingAggregate(
                        ingredient.Id,
                        ingredient.Name,
                        ingredient.Category,
                        dishIngredient.Unit);
                    items.Add(key, aggregate);
                }

                aggregate.AddAmount(dishIngredient.Quantity);
            }
        }

        var categories = items.Values
            .GroupBy(item => item.Category)
            .OrderBy(group => group.Key.ToString())
            .Select(group => new ShoppingCategory(
                group.Key.ToString(),
                group
                    .OrderBy(item => item.Name)
                    .ThenBy(item => item.Unit.ToString())
                    .Select(item => new ShoppingItem(
                        item.IngredientId.ToString("D"),
                        item.Name,
                        item.Amount,
                        item.Unit.ToString()))
                    .ToArray()))
            .ToArray();

        return categories;
    }

    // Formats date values for the API response.
    private static string FormatDate(DateOnly date) =>
        date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}

internal enum FetchOutcome
{
    Success,
    NotFound,
    Invalid
}

internal sealed record UseCaseResult(
    FetchOutcome Outcome,
    Response? ShoppingList,
    IReadOnlyList<ValidationError> Errors);

internal readonly record struct ShoppingKey(Guid IngredientId, Unit Unit);

internal sealed class ShoppingAggregate(
    Guid ingredientId,
    string name,
    IngredientCategory category,
    Unit unit)
{
    public Guid IngredientId { get; } = ingredientId;
    public string Name { get; } = name;
    public IngredientCategory Category { get; } = category;
    public Unit Unit { get; } = unit;
    public double Amount { get; private set; }

    // Adds ingredient quantities for aggregation.
    public void AddAmount(double amount)
    {
        Amount += amount;
    }
}
