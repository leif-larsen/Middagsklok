namespace Middagsklok.Contracts.Dishes.Details;

public record UpdateDishResponse(
    string Id,
    string UpdatedAt,
    IReadOnlyList<string> Warnings);
