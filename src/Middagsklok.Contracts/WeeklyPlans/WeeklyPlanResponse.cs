namespace Middagsklok.Contracts.WeeklyPlans;

public record WeeklyPlanDishDto(
    string Id,
    string Name,
    int ActiveMinutes,
    int TotalMinutes);

public record WeeklyPlanItemDto(
    int DayIndex,
    WeeklyPlanDishDto Dish);

public record WeeklyPlanResponse(
    string WeekStartDate,
    IReadOnlyList<WeeklyPlanItemDto> Items);
