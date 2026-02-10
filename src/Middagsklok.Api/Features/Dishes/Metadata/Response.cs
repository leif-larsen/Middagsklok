namespace Middagsklok.Api.Features.Dishes.Metadata;

public sealed record Response(
    IEnumerable<CuisineMetadata> Cuisines,
    IEnumerable<VibeTagMetadata> VibeTags);

public sealed record CuisineMetadata(
    string Value,
    string Label,
    int Order,
    bool IsSelectable,
    double DefaultWeightWeekday,
    double DefaultWeightWeekend,
    bool IsFallback);

public sealed record VibeTagMetadata(
    string Value,
    string Label,
    int Order,
    double WeightMultiplierWeekday,
    double WeightMultiplierWeekend);
