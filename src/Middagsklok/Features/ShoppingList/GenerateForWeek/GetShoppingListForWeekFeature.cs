using Middagsklok.Features.Shared;

namespace Middagsklok.Features.ShoppingList.GenerateForWeek;

public class GetShoppingListForWeekFeature(
    IWeeklyPlanRepository weeklyPlanRepository,
    IShoppingListGenerator shoppingListGenerator)
{
    public async Task<ShoppingList?> Execute(DateOnly weekStartDate, CancellationToken ct = default)
    {
        var plan = await weeklyPlanRepository.GetByWeekStartDate(weekStartDate, ct);
        if (plan == null)
        {
            return null;
        }

        return shoppingListGenerator.Generate(plan);
    }
}
