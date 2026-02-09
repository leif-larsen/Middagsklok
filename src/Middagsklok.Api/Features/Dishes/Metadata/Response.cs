namespace Middagsklok.Api.Features.Dishes.Metadata;

public sealed record Response(IEnumerable<CuisineMetadata> Cuisines);

public sealed record CuisineMetadata(
    string Value,
    string Label,
    int Order,
    bool IsSelectable);
