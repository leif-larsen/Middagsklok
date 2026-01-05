namespace Middagsklok.Database.Entities;

public class WeeklyPlanItemEntity
{
    public Guid PlanId { get; set; }
    public int DayIndex { get; set; }
    public Guid DishId { get; set; }

    public WeeklyPlanEntity Plan { get; set; } = null!;
    public DishEntity Dish { get; set; } = null!;
}
