namespace Middagsklok.Domain;

public record Ingredient(
    Guid Id,
    string Name,
    string Category,
    string DefaultUnit);
