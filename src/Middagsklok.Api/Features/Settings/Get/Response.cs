namespace Middagsklok.Api.Features.Settings.Get;

public sealed record Response(
    string Id,
    string WeekStartsOn,
    int SeafoodPerWeek,
    int DaysBetween);

public sealed record ErrorResponse(
    string Message,
    IReadOnlyList<ValidationError> Errors);

public sealed record ValidationError(string Field, string Message);
