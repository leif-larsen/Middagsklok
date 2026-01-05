namespace Middagsklok.Domain;

public record WeeklyPlan(
    Guid Id,
    DateOnly WeekStartDate,
    DateTimeOffset CreatedAt,
    IReadOnlyList<WeeklyPlanItem> Items);
