namespace Middagsklok.Api.Domain.Settings;

public class PlanningSettings(DayOfWeek weekStartsOn) : BaseEntity
{
    // Required by EF Core.
    private PlanningSettings()
        : this(DayOfWeek.Monday)
    {
    }

    public DayOfWeek WeekStartsOn { get; private set; } = weekStartsOn;

    // Updates the week start day setting.
    public void Update(DayOfWeek weekStartsOn)
    {
        WeekStartsOn = weekStartsOn;
        Touch();
    }
}
