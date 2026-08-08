using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.Dish;
using Middagsklok.Api.Domain.Ingredient;
using Middagsklok.Api.Domain.Settings;
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

        var isMarkedAsEaten = await IsPlanMarkedAsEaten(plan.Id, cancellationToken);
        if (isMarkedAsEaten)
        {
            var notFoundResult = new UseCaseResult(
                FetchOutcome.NotFound,
                null,
                Array.Empty<ValidationError>());
            return notFoundResult;
        }

        var plannedDays = plan.PlannedDishes
            .Where(day => day.Selection.DishId is not null)
            .ToArray();

        if (plannedDays.Length == 0)
        {
            return EmptyResult(plan.StartDate);
        }

        var dishIds = plannedDays
            .Select(day => day.Selection.DishId!.Value)
            .Distinct()
            .ToArray();

        var dishes = await LoadDishes(dishIds, cancellationToken);

        if (dishes.Count == 0)
        {
            return EmptyResult(plan.StartDate);
        }

        var dishById = dishes.ToDictionary(dish => dish.Id);
        var ingredientLookup = await LoadIngredients(dishes, cancellationToken);
        var defaultServings = await LoadDefaultServings(cancellationToken);
        var categories = BuildCategories(plannedDays, dishById, ingredientLookup, defaultServings);
        var response = new Response(FormatDate(plan.StartDate), categories);
        var result = new UseCaseResult(FetchOutcome.Success, response, Array.Empty<ValidationError>());

        return result;
    }

    // Builds the success result for a plan with nothing to shop for.
    private static UseCaseResult EmptyResult(DateOnly startDate)
    {
        var response = new Response(FormatDate(startDate), Array.Empty<ShoppingCategory>());
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

    // Checks whether the weekly plan has already been marked as eaten.
    private async Task<bool> IsPlanMarkedAsEaten(Guid planId, CancellationToken cancellationToken)
    {
        var isMarkedAsEaten = await _dbContext.DishConsumptionEvents
            .AsNoTracking()
            .AnyAsync(evt => evt.WeeklyPlanId == planId, cancellationToken);

        return isMarkedAsEaten;
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

    // Loads the household size used when a planned day does not override servings.
    private async Task<int> LoadDefaultServings(CancellationToken cancellationToken)
    {
        var settings = await _dbContext.PlanningSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        return settings?.HouseholdSize ?? PlanningSettings.DefaultHouseholdSize;
    }

    // Builds shopping list categories from the planned days.
    private static IEnumerable<ShoppingCategory> BuildCategories(
        IReadOnlyList<PlannedDay> plannedDays,
        IReadOnlyDictionary<Guid, Dish> dishById,
        IReadOnlyDictionary<Guid, Ingredient> ingredientLookup,
        int defaultServings)
    {
        var items = new Dictionary<ShoppingKey, ShoppingAggregate>();

        // Iterating days rather than distinct dishes means a dish planned twice in the
        // same week contributes its ingredients twice, each at that day's servings.
        foreach (var day in plannedDays)
        {
            if (!dishById.TryGetValue(day.Selection.DishId!.Value, out var dish))
            {
                continue;
            }

            var servings = day.Servings ?? defaultServings;

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
                        dishIngredient.Unit,
                        ingredient.IsPantryStaple);
                    items.Add(key, aggregate);
                }

                aggregate.AddAmount(dishIngredient.AmountFor(servings));
                aggregate.AddDish(dish.Name);
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
                        Round(item.Amount),
                        item.Unit.ToString(),
                        item.Dishes,
                        item.IsPantryStaple))
                    .ToArray()))
            .ToArray();

        return categories;
    }

    // Trims floating point noise from accumulated per-serving amounts.
    private static double Round(double amount) => Math.Round(amount, 4, MidpointRounding.AwayFromZero);

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
    Unit unit,
    bool isPantryStaple)
{
    private readonly List<string> _dishes = new();

    public Guid IngredientId { get; } = ingredientId;
    public string Name { get; } = name;
    public IngredientCategory Category { get; } = category;
    public Unit Unit { get; } = unit;
    public bool IsPantryStaple { get; } = isPantryStaple;
    public double Amount { get; private set; }
    public IReadOnlyList<string> Dishes => _dishes;

    // Adds ingredient quantities for aggregation.
    public void AddAmount(double amount)
    {
        Amount += amount;
    }

    // Records a dish that contributes to this ingredient, skipping duplicates.
    public void AddDish(string dishName)
    {
        if (!_dishes.Contains(dishName))
        {
            _dishes.Add(dishName);
        }
    }
}
