using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.Dish;
using Middagsklok.Api.Domain.Ingredient;
using Middagsklok.Api.Domain.WeeklyPlan;

namespace Middagsklok.Api.Features.WeeklyPlans.PlannedDishByDate;

internal sealed class UseCase(AppDbContext dbContext)
{
    private readonly AppDbContext _dbContext = dbContext;

    // Executes the planned dish retrieval workflow for a specific date.
    public async Task<UseCaseResult> Execute(string? date, CancellationToken cancellationToken)
    {
        var validator = new Validator();
        var validation = validator.Validate(date);

        if (!validation.IsValid)
        {
            var invalidResult = new UseCaseResult(
                FetchOutcome.Invalid,
                null,
                validation.ErrorMessage ?? "Invalid date format.");
            return invalidResult;
        }

        var targetDate = validation.Date;

        var weeklyPlan = await _dbContext.WeeklyPlans
            .AsNoTracking()
            .Include(wp => wp.Days)
            .Where(wp => wp.StartDate <= targetDate)
            .ToListAsync(cancellationToken);

        var matchingPlan = weeklyPlan.FirstOrDefault(wp => targetDate <= wp.EndDate);

        if (matchingPlan is null)
        {
            var notFoundResult = new UseCaseResult(
                FetchOutcome.NotFound,
                null,
                "No weekly plan found for the specified date.");
            return notFoundResult;
        }

        var plannedDay = matchingPlan.Days.FirstOrDefault(day => day.Date == targetDate);

        if (plannedDay is null || plannedDay.Selection.Type != DishSelectionType.Dish || !plannedDay.Selection.DishId.HasValue)
        {
            var noDishResult = new UseCaseResult(
                FetchOutcome.NotFound,
                null,
                "No dish planned for the specified date.");
            return noDishResult;
        }

        var dish = await _dbContext.Dishes
            .AsNoTracking()
            .Include(d => d.Ingredients)
            .FirstOrDefaultAsync(d => d.Id == plannedDay.Selection.DishId.Value, cancellationToken);

        if (dish is null)
        {
            var dishNotFoundResult = new UseCaseResult(
                FetchOutcome.NotFound,
                null,
                "Planned dish not found in database.");
            return dishNotFoundResult;
        }

        var ingredientIds = dish.Ingredients
            .Select(i => i.IngredientId)
            .Distinct()
            .ToArray();

        var ingredientLookup = await LoadIngredients(ingredientIds, cancellationToken);

        var response = MapDishResponse(targetDate, dish, ingredientLookup);
        var result = new UseCaseResult(FetchOutcome.Success, response, null);

        return result;
    }

    // Loads ingredients by id for complete ingredient information.
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

    // Maps a dish entity to the response with full ingredient details.
    private static Response MapDishResponse(
        DateOnly date,
        Dish dish,
        IReadOnlyDictionary<Guid, Ingredient> ingredientLookup)
    {
        var ingredients = dish.Ingredients
            .OrderBy(i => i.SortOrder ?? int.MaxValue)
            .Select(ingredient =>
            {
                var ingredientName = ingredientLookup.TryGetValue(ingredient.IngredientId, out var ingredientEntity)
                    ? ingredientEntity.Name
                    : "Unknown";

                return new IngredientDetails(
                    ingredient.IngredientId.ToString("D"),
                    ingredientName,
                    ingredient.Quantity,
                    FormatUnit(ingredient.Unit),
                    ingredient.Note);
            })
            .ToArray();

        var dishDetails = new DishDetails(
            dish.Id.ToString("D"),
            dish.Name,
            dish.DishType.ToString(),
            dish.PrepTimeMinutes,
            dish.CookTimeMinutes,
            dish.Servings,
            dish.Instructions,
            dish.IsSeafood,
            dish.IsVegetarian,
            dish.IsVegan,
            dish.VibeTags.ToArray(),
            ingredients);

        var response = new Response(
            FormatDate(date),
            dishDetails);

        return response;
    }

    // Formats unit values for the response.
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
    Response? Data,
    string? ErrorMessage);
