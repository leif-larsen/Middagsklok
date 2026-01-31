namespace Middagsklok.Api.Domain.Settings;

public class PlanningSettings(DayOfWeek weekStartsOn, int seafoodPerWeek = 2) : BaseEntity
{
    // Required by EF Core.
    private PlanningSettings()
        : this(DayOfWeek.Monday, 2)
    {
    }

    public DayOfWeek WeekStartsOn { get; private set; } = weekStartsOn;

    public int SeafoodPerWeek { get; private set; } = seafoodPerWeek;

    // Updates the planning settings values.
    public void Update(DayOfWeek weekStartsOn, int seafoodPerWeek)
    {
        WeekStartsOn = weekStartsOn;
        SeafoodPerWeek = seafoodPerWeek;
        Touch();
    }
}
