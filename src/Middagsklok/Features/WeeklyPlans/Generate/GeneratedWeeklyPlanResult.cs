using Middagsklok.Domain;

namespace Middagsklok.Features.WeeklyPlans.Generate;

public record GeneratedWeeklyPlanResult(
    WeeklyPlan Plan,
    IReadOnlyDictionary<int, PlannedDishExplanation> ExplanationsByDay);
