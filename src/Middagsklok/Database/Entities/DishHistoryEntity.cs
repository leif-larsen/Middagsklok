namespace Middagsklok.Database.Entities;

public class DishHistoryEntity
{
    public Guid Id { get; set; }
    public Guid DishId { get; set; }
    public DateOnly Date { get; set; }
    public int? RatingOverride { get; set; }
    public string? Notes { get; set; }

    public DishEntity Dish { get; set; } = null!;
}
