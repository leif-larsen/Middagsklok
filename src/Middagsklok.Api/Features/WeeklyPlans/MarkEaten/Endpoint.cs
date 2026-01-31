namespace Middagsklok.Api.Features.WeeklyPlans.MarkEaten;

internal static class WeeklyPlansMarkEatenEndpoint
{
    // Maps the weekly plan mark-as-eaten endpoint.
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/weekly-plans/{startDate}/mark-eaten", Handle)
            .WithName("MarkWeeklyPlanEaten");
    }

    // Handles the HTTP request and delegates to the use case.
    private static async Task<IResult> Handle(
        string? startDate,
        UseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(startDate, cancellationToken);

        return result.Outcome switch
        {
            MarkEatenOutcome.Success when result.Created =>
                Results.Created($"/weekly-plans/{startDate}/mark-eaten", null),
            MarkEatenOutcome.Success =>
                Results.Ok(),
            MarkEatenOutcome.NotFound =>
                Results.NotFound(new ErrorResponse("Weekly plan not found.", result.Errors)),
            MarkEatenOutcome.AlreadyMarked =>
                Results.Conflict(new ErrorResponse("Weekly plan already marked as eaten.", result.Errors)),
            _ =>
                Results.BadRequest(new ErrorResponse("Validation failed.", result.Errors))
        };
    }
}
