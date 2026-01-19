using Middagsklok.Domain;

namespace Middagsklok.Features.WeeklyPlans.Create;

public interface IWeeklyPlanRepository
{
    Task<WeeklyPlan> CreateOrReplace(WeeklyPlan plan, CancellationToken ct = default);
}
