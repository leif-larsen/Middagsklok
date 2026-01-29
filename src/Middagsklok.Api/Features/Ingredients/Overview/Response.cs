namespace Middagsklok.Api.Features.Ingredients.Overview;

public sealed record Response(IEnumerable<IngredientOverview> Ingredients);

public sealed record IngredientOverview(
    string Id,
    string Name,
    string Category,
    string DefaultUnit,
    int UsedIn);
