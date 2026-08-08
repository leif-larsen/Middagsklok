namespace Middagsklok.Api.Domain.WeeklyPlan;

public class WeeklyPlan(DateOnly startDate, IEnumerable<PlannedDay> days) : BaseEntity
{
    private readonly List<PlannedDay> _days = days.ToList();

    // Required by EF Core.
    private WeeklyPlan(DateOnly startDate)
        : this(startDate, Array.Empty<PlannedDay>())
    {
    }

    public DateOnly StartDate { get; private set; } = startDate;
    public IReadOnlyList<PlannedDay> Days => _days;

    public DateOnly EndDate => StartDate.AddDays(6);
    public IEnumerable<PlannedDay> PlannedDishes => _days.Where(day => day.Selection.Type == DishSelectionType.Dish);

    // Updates the weekly plan start date and days.
    public void Update(DateOnly startDate, IEnumerable<PlannedDay> days)
    {
        StartDate = startDate;
        _days.Clear();
        _days.AddRange(days);
        Touch();
    }
}

public sealed record class PlannedDay
{
    // Required by EF Core.
    private PlannedDay()
    {
    }

    // Creates a planned day with a date, selection and optional servings override.
    public PlannedDay(DateOnly date, DishSelection selection, int? servings = null)
    {
        Date = date;
        Selection = selection;
        Servings = servings;
    }

    public DateOnly Date { get; private set; }
    public DishSelection Selection { get; private set; } = null!;

    // Overrides the household default for this meal, e.g. when guests are coming.
    public int? Servings { get; private set; }
}

public sealed record class DishSelection
{
    // Required by EF Core.
    private DishSelection()
    {
    }

    // Creates a dish selection with type and optional dish id.
    public DishSelection(DishSelectionType type, Guid? dishId)
    {
        Type = type;
        DishId = dishId;
    }

    public DishSelectionType Type { get; private set; }
    public Guid? DishId { get; private set; }
}

public enum DishSelectionType
{
    Empty,
    Dish
}
