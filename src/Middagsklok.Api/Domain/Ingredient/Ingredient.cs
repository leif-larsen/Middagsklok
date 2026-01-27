namespace Middagsklok.Api.Domain.Ingredient;

public class Ingredient(string name, IngredientCategory category, Unit defaultUnit) : BaseEntity
{
    public string Name { get; } = name.Trim();
    public IngredientCategory Category { get; } = category;
    public Unit DefaultUnit { get; } = defaultUnit;
}

public enum IngredientCategory
{
    Produce,
    Meat,
    Poultry,
    Seafood,
    DairyAndEggs,
    PastaAndGrains,
    Bakery,
    CannedGoods,
    FrozenFoods,
    Condiments,
    SpicesAndHerbs,
    Baking,
    OilsAndVinegars,
    Beverages,
    Snacks,
    Other
}

public enum Unit
{
    G,
    Pcs,
    Ml,
    L,
    Kg
}