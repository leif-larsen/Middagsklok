using Middagsklok.Domain;

namespace Middagsklok.Features.WeeklyPlans.Suggest;

public record SuggestWeeklyPlanRequest(
    DateOnly WeekStartDate,
    PlanningRules? Rules = null);
