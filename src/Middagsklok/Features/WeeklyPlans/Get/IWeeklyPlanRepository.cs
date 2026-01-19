using Middagsklok.Domain;

namespace Middagsklok.Features.WeeklyPlans.Get;

public interface IWeeklyPlanRepository
{
    Task<WeeklyPlan?> GetByWeekStartDate(DateOnly weekStart, CancellationToken ct = default);
}
