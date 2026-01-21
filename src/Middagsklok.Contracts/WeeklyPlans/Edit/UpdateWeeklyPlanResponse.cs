namespace Middagsklok.Contracts.WeeklyPlans.Edit;

/// <summary>
/// Response for PUT /weekly-plan/{weekStartDate} indicating update status.
/// </summary>
public record UpdateWeeklyPlanResponse(
    string WeekStartDate,
    string Status,
    IReadOnlyList<RuleViolationDto> Violations);
