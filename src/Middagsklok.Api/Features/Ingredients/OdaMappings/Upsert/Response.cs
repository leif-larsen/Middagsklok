namespace Middagsklok.Api.Features.Ingredients.OdaMappings.Upsert;

public sealed record Response(
    string IngredientId,
    string IngredientName,
    string Availability,
    int? ProductId,
    string? ProductName,
    double? PackQuantity,
    string? PackUnit,
    bool Confirmed);

public sealed record ErrorResponse(
    string Message,
    IReadOnlyList<ValidationError> Errors);

public sealed record ValidationError(string Field, string Message);
