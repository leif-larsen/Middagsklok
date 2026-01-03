namespace Middagsklok.Database.Entities;

public class DishIngredientEntity
{
    public Guid DishId { get; set; }
    public Guid IngredientId { get; set; }
    public decimal Amount { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool Optional { get; set; }

    public DishEntity Dish { get; set; } = null!;
    public IngredientEntity Ingredient { get; set; } = null!;
}
