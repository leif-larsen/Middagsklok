namespace Middagsklok.Contracts.WeeklyPlans;

public record SaveWeeklyPlanItemDto(
    int DayIndex,
    string DishId);

public record SaveWeeklyPlanRequest(
    string WeekStartDate,
    IReadOnlyList<SaveWeeklyPlanItemDto> Items);
