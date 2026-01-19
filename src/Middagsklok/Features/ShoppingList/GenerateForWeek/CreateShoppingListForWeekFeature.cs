using Middagsklok.Domain;

namespace Middagsklok.Features.ShoppingList.GenerateForWeek;

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

        return GetShoppingListFeature.Execute(plan);
    }
}
