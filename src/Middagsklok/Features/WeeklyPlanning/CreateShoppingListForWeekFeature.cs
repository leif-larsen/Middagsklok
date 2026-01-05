using Middagsklok.Database.Repositories;
using Middagsklok.Features.GetShoppingList;

namespace Middagsklok.Features.WeeklyPlanning;

public class CreateShoppingListForWeekFeature
{
    private readonly IWeeklyPlanRepository _weeklyPlanRepository;

    public CreateShoppingListForWeekFeature(IWeeklyPlanRepository weeklyPlanRepository)
    {
        _weeklyPlanRepository = weeklyPlanRepository;
    }

    public async Task<ShoppingList?> Execute(DateOnly weekStartDate, CancellationToken ct = default)
    {
        var plan = await _weeklyPlanRepository.GetByWeekStartDate(weekStartDate, ct);
        
        if (plan is null)
            return null;

        var aggregated = plan.Items
            .SelectMany(item => item.Dish.Ingredients)
            .Where(di => !di.Optional)
            .GroupBy(di => (di.Ingredient.Id, di.Unit))
            .Select(group =>
            {
                var first = group.First();
                return new ShoppingListItem(
                    IngredientName: first.Ingredient.Name,
                    Category: first.Ingredient.Category,
                    Amount: group.Sum(di => di.Amount),
                    Unit: first.Unit);
            })
            .OrderBy(item => item.Category)
            .ThenBy(item => item.IngredientName)
            .ToList();

        return new ShoppingList(aggregated);
    }
}
