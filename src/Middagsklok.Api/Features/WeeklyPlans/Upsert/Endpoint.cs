namespace Middagsklok.Api.Features.WeeklyPlans.Upsert;

internal static class WeeklyPlansUpsertEndpoint
{
    // Maps the weekly plan upsert endpoint.
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/weekly-plans/{startDate}", Handle)
            .WithName("UpsertWeeklyPlan");
    }

    // Handles the HTTP request and delegates to the use case.
    private static async Task<IResult> Handle(
        string? startDate,
        Request? request,
        UseCase useCase,
        CancellationToken cancellationToken)
    {
        var safeRequest = request ?? new Request(null);
        var result = await useCase.Execute(startDate, safeRequest, cancellationToken);

        return result.Outcome switch
        {
            UpsertOutcome.Success when result.Plan is not null =>
                Results.Ok(result.Plan),
            _ =>
                Results.BadRequest(new ErrorResponse("Validation failed.", result.Errors))
        };
    }
}
