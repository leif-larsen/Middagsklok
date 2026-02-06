namespace Middagsklok.Api.Domain.Settings;

public class PlanningSettings(
    DayOfWeek weekStartsOn,
    int seafoodPerWeek = 2,
    int daysBetween = 14) : BaseEntity
{
    // Required by EF Core.
    private PlanningSettings()
        : this(DayOfWeek.Monday, 2, 14)
    {
    }

    public DayOfWeek WeekStartsOn { get; private set; } = weekStartsOn;

    public int SeafoodPerWeek { get; private set; } = seafoodPerWeek;

    public int DaysBetween { get; private set; } = daysBetween;

    // Updates the planning settings values.
    public void Update(DayOfWeek weekStartsOn, int seafoodPerWeek, int daysBetween)
    {
        WeekStartsOn = weekStartsOn;
        SeafoodPerWeek = seafoodPerWeek;
        DaysBetween = daysBetween;
        Touch();
    }
}
