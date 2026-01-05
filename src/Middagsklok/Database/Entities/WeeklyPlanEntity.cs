namespace Middagsklok.Database.Entities;

public class WeeklyPlanEntity
{
    public Guid Id { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public List<WeeklyPlanItemEntity> Items { get; set; } = [];
}
