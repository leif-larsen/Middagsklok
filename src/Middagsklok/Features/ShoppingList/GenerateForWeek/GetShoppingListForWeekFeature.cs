namespace Middagsklok.Features.ShoppingList.GenerateForWeek;

public class GetShoppingListForWeekFeature
{
    private readonly IWeeklyPlanRepository _weeklyPlanRepository;

    public GetShoppingListForWeekFeature(IWeeklyPlanRepository weeklyPlanRepository)
    {
        _weeklyPlanRepository = weeklyPlanRepository;
    }

    public async Task<ShoppingList?> Execute(DateOnly weekStartDate, CancellationToken ct = default)
    {
        var plan = await _weeklyPlanRepository.GetByWeekStartDate(weekStartDate, ct);
        if (plan == null)
        {
            return null;
        }

        return GetShoppingListFeature.Execute(plan);
    }
}
