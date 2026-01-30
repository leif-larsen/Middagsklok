namespace Middagsklok.Api.Domain.Ingredient;

public class Ingredient(string name, IngredientCategory category, Unit defaultUnit) : BaseEntity
{
    public string Name { get; private set; } = name.Trim();
    public IngredientCategory Category { get; private set; } = category;
    public Unit DefaultUnit { get; private set; } = defaultUnit;

    // Updates the ingredient details.
    public void Update(string name, IngredientCategory category, Unit defaultUnit)
    {
        Name = name.Trim();
        Category = category;
        DefaultUnit = defaultUnit;
        Touch();
    }
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
