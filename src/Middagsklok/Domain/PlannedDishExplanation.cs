namespace Middagsklok.Domain;

public record PlannedDishExplanation(
    Guid DishId,
    IReadOnlyList<string> Reasons);
