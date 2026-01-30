namespace Middagsklok.Api.Features.Ingredients.Create;

public sealed record Response(
    string Id,
    string Name,
    string Category,
    string DefaultUnit,
    int UsedIn);

public sealed record ErrorResponse(
    string Message,
    IReadOnlyList<ValidationError> Errors);

public sealed record ValidationError(string Field, string Message);
