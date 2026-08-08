namespace Middagsklok.Api.Domain.Settings;

public class PlanningSettings(
    DayOfWeek weekStartsOn,
    int seafoodPerWeek = 2,
    int daysBetween = 14,
    // Kept as a literal because a primary constructor default cannot reference
    // a constant declared in the same class body.
    int householdSize = 3) : BaseEntity
{
    public const int DefaultHouseholdSize = 3;

    // Required by EF Core.
    private PlanningSettings()
        : this(DayOfWeek.Monday, 2, 14, DefaultHouseholdSize)
    {
    }

    public DayOfWeek WeekStartsOn { get; private set; } = weekStartsOn;

    public int SeafoodPerWeek { get; private set; } = seafoodPerWeek;

    public int DaysBetween { get; private set; } = daysBetween;

    // Default number of servings a planned meal is scaled to when the day does not override it.
    public int HouseholdSize { get; private set; } = householdSize;

    // Updates the planning settings values.
    public void Update(
        DayOfWeek weekStartsOn,
        int seafoodPerWeek,
        int daysBetween,
        int householdSize = DefaultHouseholdSize)
    {
        WeekStartsOn = weekStartsOn;
        SeafoodPerWeek = seafoodPerWeek;
        DaysBetween = daysBetween;
        HouseholdSize = householdSize;
        Touch();
    }
}
