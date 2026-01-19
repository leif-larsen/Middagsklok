using Middagsklok.Domain;

namespace Middagsklok.Features.WeeklyPlans.Generate;

public interface IWeeklyPlanRepository
{
    Task<WeeklyPlan> CreateOrReplace(WeeklyPlan plan, CancellationToken ct = default);
}
