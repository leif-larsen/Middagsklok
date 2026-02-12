namespace Middagsklok.Api.Features.Dishes.Lookup;

public sealed record Response(IEnumerable<DishLookup> Dishes);

public sealed record DishLookup(
    string Id,
    string Name,
    string DishType,
    IReadOnlyList<string> VibeTags);
