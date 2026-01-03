namespace Middagsklok.Domain;

public record WeeklyPlan(
    DateOnly WeekStartDate,
    List<WeeklyPlanItem> Items);
