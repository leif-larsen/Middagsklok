namespace Middagsklok.Api.Features.Ingredients.Metadata;

public sealed record Response(
    IEnumerable<IngredientCategoryMetadata> Categories,
    IEnumerable<IngredientUnitMetadata> Units);

public sealed record IngredientCategoryMetadata(
    string Value,
    string Label);

public sealed record IngredientUnitMetadata(
    string Value,
    string Label);
