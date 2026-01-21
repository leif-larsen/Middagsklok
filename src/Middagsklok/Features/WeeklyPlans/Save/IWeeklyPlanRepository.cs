using Middagsklok.Domain;

namespace Middagsklok.Features.WeeklyPlans.Save;

public interface IWeeklyPlanRepository
{
    Task<WeeklyPlan> CreateOrReplace(WeeklyPlan plan, CancellationToken ct = default);
}
