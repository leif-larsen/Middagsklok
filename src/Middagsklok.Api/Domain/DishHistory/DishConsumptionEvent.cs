namespace Middagsklok.Api.Domain.DishHistory;

public class DishConsumptionEvent(Guid dishId, DateOnly eatenOn, DishHistorySource source, Guid? weeklyPlanId) : BaseEntity
{
    // Required by EF Core.
    private DishConsumptionEvent()
        : this(Guid.Empty, default, default, null)
    {
    }

    public Guid DishId { get; private set; } = dishId;
    public DateOnly EatenOn { get; private set; } = eatenOn;
    public DishHistorySource Source { get; private set; } = source;
    public Guid? WeeklyPlanId { get; private set; } = weeklyPlanId;
}

public enum DishHistorySource
{
    WeeklyPlan,
    Manual
}
