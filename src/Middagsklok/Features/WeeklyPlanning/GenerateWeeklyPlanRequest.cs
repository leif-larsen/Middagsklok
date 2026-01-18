using Middagsklok.Domain;

namespace Middagsklok.Features.WeeklyPlanning;

public record GenerateWeeklyPlanRequest(
    DateOnly WeekStartDate,
    PlanningRules? Rules = null);
