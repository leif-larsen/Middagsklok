using Middagsklok.Domain;

namespace Middagsklok.Features.WeeklyPlans.Save;

public record SaveWeeklyPlanItemRequest(
    int DayIndex,
    Guid DishId);

public record SaveWeeklyPlanRequest(
    DateOnly WeekStartDate,
    IReadOnlyList<SaveWeeklyPlanItemRequest> Items);
