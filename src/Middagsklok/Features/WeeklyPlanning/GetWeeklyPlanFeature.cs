using Middagsklok.Database.Repositories;
using Middagsklok.Domain;

namespace Middagsklok.Features.WeeklyPlanning;

public class GetWeeklyPlanFeature
{
    private readonly IWeeklyPlanRepository _weeklyPlanRepository;

    public GetWeeklyPlanFeature(IWeeklyPlanRepository weeklyPlanRepository)
    {
        _weeklyPlanRepository = weeklyPlanRepository;
    }

    public Task<WeeklyPlan?> Execute(DateOnly weekStartDate, CancellationToken ct = default)
    {
        return _weeklyPlanRepository.GetByWeekStartDate(weekStartDate, ct);
    }
}
