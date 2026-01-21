namespace Middagsklok.Features.WeeklyPlans.Edit;

public record EditWeeklyPlanRequest(
    DateOnly WeekStartDate,
    IReadOnlyList<EditWeeklyPlanItemRequest> Items);

public record EditWeeklyPlanItemRequest(
    int DayIndex,
    Guid DishId);
