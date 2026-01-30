namespace Middagsklok.Api.Features.Settings.Upsert;

public sealed record Response(
    string Id,
    string WeekStartsOn);

public sealed record ErrorResponse(
    string Message,
    IReadOnlyList<ValidationError> Errors);

public sealed record ValidationError(string Field, string Message);
