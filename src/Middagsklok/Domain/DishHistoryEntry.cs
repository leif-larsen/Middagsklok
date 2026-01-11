namespace Middagsklok.Domain;

public record DishHistoryEntry(
    Guid Id,
    Guid DishId,
    DateOnly Date,
    int? RatingOverride,
    string? Notes);
