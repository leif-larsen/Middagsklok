namespace Middagsklok.Contracts.WeeklyPlans;

public record SuggestWeeklyPlanResponse(
    string WeekStartDate,
    IReadOnlyList<WeeklyPlanItemDto> Items,
    Dictionary<int, PlannedDishExplanationDto> ExplanationsByDay,
    IReadOnlyList<RuleViolationDto> Violations);
