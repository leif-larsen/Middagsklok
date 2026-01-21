namespace Middagsklok.Contracts.WeeklyPlans.Edit;

/// <summary>
/// Item in the weekly plan edit response.
/// </summary>
public record WeeklyPlanEditItemDto(
    int DayIndex,
    string DishId,
    string DishName,
    int ActiveMinutes,
    int TotalMinutes);
