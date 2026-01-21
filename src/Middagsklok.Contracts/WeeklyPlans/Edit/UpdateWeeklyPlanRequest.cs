namespace Middagsklok.Contracts.WeeklyPlans.Edit;

/// <summary>
/// Request for PUT /weekly-plan/{weekStartDate} to update a weekly plan.
/// </summary>
public record UpdateWeeklyPlanRequest(
    string WeekStartDate,
    IReadOnlyList<UpdateWeeklyPlanItemDto> Items);
