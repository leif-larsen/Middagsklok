namespace Middagsklok.Domain;

public record GeneratedWeeklyPlanResult(
    WeeklyPlan Plan,
    IReadOnlyDictionary<int, PlannedDishExplanation> ExplanationsByDay);
