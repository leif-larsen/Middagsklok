using Middagsklok.Domain;

namespace Middagsklok.Features.WeeklyPlans.Edit;

public interface IWeeklyPlanRepository
{
    Task<WeeklyPlan> CreateOrReplace(WeeklyPlan plan, CancellationToken ct = default);
}
