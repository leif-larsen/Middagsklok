namespace Middagsklok.Api.Features.WeeklyPlans.PlannedDishByDate;

internal static class PlannedDishByDateEndpoint
{
    // Maps the planned dish by date retrieval endpoint.
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/weekly-plans/dish/{date}", Handle)
            .WithName("GetPlannedDishByDate");
    }

    // Handles the HTTP request and delegates to the use case.
    private static async Task<IResult> Handle(
        string? date,
        UseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(date, cancellationToken);

        return result.Outcome switch
        {
            FetchOutcome.Success when result.Data is not null =>
                Results.Ok(result.Data),
            FetchOutcome.NotFound =>
                Results.NotFound(new ErrorResponse(result.ErrorMessage ?? "Not found.")),
            _ =>
                Results.BadRequest(new ErrorResponse(result.ErrorMessage ?? "Invalid request."))
        };
    }
}
