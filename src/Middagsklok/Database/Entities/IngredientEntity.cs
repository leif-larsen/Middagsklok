namespace Middagsklok.Database.Entities;

public class IngredientEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string DefaultUnit { get; set; } = string.Empty;

    public List<DishIngredientEntity> DishIngredients { get; set; } = [];
}
