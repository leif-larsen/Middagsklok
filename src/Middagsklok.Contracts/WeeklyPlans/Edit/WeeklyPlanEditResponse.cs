namespace Middagsklok.Contracts.WeeklyPlans.Edit;

/// <summary>
/// Response for GET /weekly-plan/{weekStartDate}/edit with optional violations.
/// </summary>
public record WeeklyPlanEditResponse(
    string WeekStartDate,
    IReadOnlyList<WeeklyPlanEditItemDto> Items,
    IReadOnlyList<RuleViolationDto>? Violations);
