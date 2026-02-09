namespace Middagsklok.Api.Features.WeeklyPlans.ByStartDate;

public sealed record Response(
    string Id,
    string StartDate,
    IEnumerable<PlannedDayResponse> Days,
    bool IsMarkedAsEaten);

public sealed record PlannedDayResponse(
    string Date,
    DishSelectionResponse Selection);

public sealed record DishSelectionResponse(
    string Type,
    string? DishId);

public sealed record ErrorResponse(
    string Message,
    IReadOnlyList<ValidationError> Errors);

public sealed record ValidationError(string Field, string Message);
