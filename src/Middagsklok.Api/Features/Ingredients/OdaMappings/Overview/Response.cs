namespace Middagsklok.Api.Features.Ingredients.OdaMappings.Overview;

public sealed record Response(
    IEnumerable<MappedIngredient> Mappings,
    IEnumerable<UnmappedIngredient> Unmapped);

public sealed record MappedIngredient(
    string IngredientId,
    string IngredientName,
    string Availability,
    int? ProductId,
    string? ProductName,
    double? PackQuantity,
    string? PackUnit,
    bool Confirmed,
    int UsedIn);

public sealed record UnmappedIngredient(
    string IngredientId,
    string IngredientName,
    string Category,
    string DefaultUnit,
    bool IsPantryStaple,
    int UsedIn);
