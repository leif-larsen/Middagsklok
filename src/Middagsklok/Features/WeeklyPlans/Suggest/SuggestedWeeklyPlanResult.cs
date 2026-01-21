using Middagsklok.Domain;

namespace Middagsklok.Features.WeeklyPlans.Suggest;

public record SuggestedWeeklyPlanResult(
    WeeklyPlan Plan,
    IReadOnlyDictionary<int, PlannedDishExplanation> ExplanationsByDay,
    IReadOnlyList<RuleViolation> Violations);
