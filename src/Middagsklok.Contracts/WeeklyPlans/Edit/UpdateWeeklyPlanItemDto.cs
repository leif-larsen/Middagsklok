namespace Middagsklok.Contracts.WeeklyPlans.Edit;

/// <summary>
/// Item in the update weekly plan request.
/// </summary>
public record UpdateWeeklyPlanItemDto(
    int DayIndex,
    string DishId);
