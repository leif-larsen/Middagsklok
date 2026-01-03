namespace Middagsklok.Database.Entities;

public class DishEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ActiveMinutes { get; set; }
    public int TotalMinutes { get; set; }
    public int KidRating { get; set; }
    public int FamilyRating { get; set; }
    public bool IsPescetarian { get; set; }
    public bool HasOptionalMeatVariant { get; set; }

    public List<DishIngredientEntity> DishIngredients { get; set; } = [];
}
