namespace Middagsklok.Contracts.WeeklyPlans;

public record PlannedDishExplanationDto(
    string DishId,
    IReadOnlyList<string> Reasons);

public record GenerateWeeklyPlanResponse(
    WeeklyPlanResponse Plan,
    Dictionary<int, PlannedDishExplanationDto> ExplanationsByDay);
