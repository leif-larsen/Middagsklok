namespace Middagsklok.Api.Features.Dishes.Import;

public sealed record Response(
    int Attempted,
    int Imported,
    int Skipped,
    int Failed,
    IReadOnlyList<Failure> Failures);

public sealed record Failure(string? DishName, string Reason, string? IngredientName = null);
