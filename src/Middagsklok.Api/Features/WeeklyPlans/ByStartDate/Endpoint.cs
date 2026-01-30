namespace Middagsklok.Api.Features.WeeklyPlans.ByStartDate;

internal static class WeeklyPlansByStartDateEndpoint
{
    // Maps the weekly plan retrieval endpoint.
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/weekly-plans/{startDate}", Handle)
            .WithName("GetWeeklyPlan");
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
            FetchOutcome.Success when result.Plan is not null =>
                Results.Ok(result.Plan),
            FetchOutcome.NotFound =>
                Results.NotFound(new ErrorResponse("Weekly plan not found.", result.Errors)),
            _ =>
                Results.BadRequest(new ErrorResponse("Validation failed.", result.Errors))
        };
    }
}
