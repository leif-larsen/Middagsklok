namespace Middagsklok.Contracts.WeeklyPlans;

public record SaveWeeklyPlanResponse(
    string WeekStartDate,
    string Status,
    IReadOnlyList<RuleViolationDto> Violations);
