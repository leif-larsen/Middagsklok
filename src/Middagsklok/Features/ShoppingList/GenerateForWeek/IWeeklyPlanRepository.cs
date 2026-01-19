using Middagsklok.Domain;

namespace Middagsklok.Features.ShoppingList.GenerateForWeek;

public interface IWeeklyPlanRepository
{
    Task<WeeklyPlan?> GetByWeekStartDate(DateOnly weekStart, CancellationToken ct = default);
}
