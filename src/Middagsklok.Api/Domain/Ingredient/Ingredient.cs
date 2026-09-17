namespace Middagsklok.Api.Domain.Ingredient;

public class Ingredient(
    string name,
    IngredientCategory category,
    Unit defaultUnit,
    bool isPantryStaple = false) : BaseEntity
{
    public string Name { get; private set; } = name.Trim();
    public IngredientCategory Category { get; private set; } = category;
    public Unit DefaultUnit { get; private set; } = defaultUnit;

    // Pantry staples are excluded from shopping suggestions because they are always in stock.
    public bool IsPantryStaple { get; private set; } = isPantryStaple;

    // The Oda product this ingredient resolves to, or a record that Oda does not stock it.
    // Null means nobody has looked yet.
    public OdaProductMapping? OdaMapping { get; private set; }

    // Updates the ingredient details.
    public void Update(string name, IngredientCategory category, Unit defaultUnit, bool isPantryStaple = false)
    {
        Name = name.Trim();
        Category = category;
        DefaultUnit = defaultUnit;
        IsPantryStaple = isPantryStaple;
        Touch();
    }

    // Sets or replaces the Oda mapping for this ingredient.
    public void SetOdaMapping(OdaProductMapping mapping)
    {
        OdaMapping = mapping;
        Touch();
    }

    // Removes the Oda mapping so the ingredient counts as unmapped again.
    public void ClearOdaMapping()
    {
        OdaMapping = null;
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
    Kg,

    // A fraction of one retail package, used when the natural unit is "most of a bag"
    // rather than an absolute weight. Aggregated amounts round up to whole packages.
    Pack
}
