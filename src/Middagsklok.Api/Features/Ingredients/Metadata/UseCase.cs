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
            IngredientCategory.Produce => "Frukt og grønt",
            IngredientCategory.Meat => "Kjøtt",
            IngredientCategory.Poultry => "Fjærkre",
            IngredientCategory.Seafood => "Sjømat",
            IngredientCategory.DairyAndEggs => "Meieri og egg",
            IngredientCategory.PastaAndGrains => "Pasta og korn",
            IngredientCategory.Bakery => "Bakervarer",
            IngredientCategory.CannedGoods => "Hermetikk",
            IngredientCategory.FrozenFoods => "Frossenmat",
            IngredientCategory.Condiments => "Sauser og dressinger",
            IngredientCategory.SpicesAndHerbs => "Krydder og urter",
            IngredientCategory.Baking => "Baking",
            IngredientCategory.OilsAndVinegars => "Oljer og eddik",
            IngredientCategory.Beverages => "Drikke",
            IngredientCategory.Snacks => "Snacks",
            IngredientCategory.Other => "Annet",
            _ => "Annet"
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
