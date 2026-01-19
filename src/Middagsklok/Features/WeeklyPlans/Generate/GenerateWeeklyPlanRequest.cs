using Middagsklok.Domain;

namespace Middagsklok.Features.WeeklyPlans.Generate;

public record GenerateWeeklyPlanRequest(
    DateOnly WeekStartDate,
    PlanningRules? Rules = null);
