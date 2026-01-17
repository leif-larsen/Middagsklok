namespace Middagsklok.Domain;

public record RuleViolation(
    string RuleCode,
    string Message,
    IReadOnlyList<int> DayIndices,
    IReadOnlyList<Guid> DishIds);
