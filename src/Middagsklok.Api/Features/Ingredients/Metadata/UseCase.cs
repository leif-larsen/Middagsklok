using Middagsklok.Api.Domain.Ingredient;

namespace Middagsklok.Api.Features.Ingredients.Metadata;

internal sealed class UseCase
{
    // Executes the metadata query for ingredient categories and units.
    public Task<Response> Execute(CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        var categories = Enum.GetValues<IngredientCategory>()
            .Select(category => new IngredientCategoryMetadata(
                category.ToString(),
                FormatCategoryLabel(category)))
            .ToArray();

        var units = Enum.GetValues<Unit>()
            .Select(unit => new IngredientUnitMetadata(
                unit.ToString(),
                FormatUnitLabel(unit)))
            .ToArray();

        var response = new Response(categories, units);

        return Task.FromResult(response);
    }

    // Formats category values into display labels.
    private static string FormatCategoryLabel(IngredientCategory category) =>
        category switch
        {
            IngredientCategory.Produce => "Produce",
            IngredientCategory.Meat => "Meat",
            IngredientCategory.Poultry => "Poultry",
            IngredientCategory.Seafood => "Seafood",
            IngredientCategory.DairyAndEggs => "Dairy & Eggs",
            IngredientCategory.PastaAndGrains => "Pasta & Grains",
            IngredientCategory.Bakery => "Bakery",
            IngredientCategory.CannedGoods => "Canned Goods",
            IngredientCategory.FrozenFoods => "Frozen Foods",
            IngredientCategory.Condiments => "Condiments",
            IngredientCategory.SpicesAndHerbs => "Spices & Herbs",
            IngredientCategory.Baking => "Baking",
            IngredientCategory.OilsAndVinegars => "Oils & Vinegars",
            IngredientCategory.Beverages => "Beverages",
            IngredientCategory.Snacks => "Snacks",
            IngredientCategory.Other => "Other",
            _ => "Other"
        };

    // Formats unit values into display labels.
    private static string FormatUnitLabel(Unit unit) =>
        unit switch
        {
            Unit.G => "g",
            Unit.Kg => "kg",
            Unit.Ml => "ml",
            Unit.L => "l",
            Unit.Pcs => "pcs",
            _ => string.Empty
        };
}
