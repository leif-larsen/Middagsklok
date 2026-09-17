namespace Middagsklok.Api.Features.Ingredients.OdaMappings.Delete;

public sealed record ErrorResponse(
    string Message,
    IReadOnlyList<ValidationError> Errors);

public sealed record ValidationError(string Field, string Message);
