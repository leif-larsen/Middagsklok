namespace Middagsklok.Api.Features.Settings.Get;

internal static class PlanningSettingsGetEndpoint
{
    // Maps the planning settings retrieval endpoint.
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/planning-settings", Handle)
            .WithName("GetPlanningSettings");
    }

    // Handles the HTTP request and delegates to the use case.
    private static async Task<IResult> Handle(
        UseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(cancellationToken);

        return result.Outcome switch
        {
            FetchOutcome.Success when result.Settings is not null =>
                Results.Ok(result.Settings),
            FetchOutcome.NotFound =>
                Results.NotFound(new ErrorResponse("Planning settings not found.", result.Errors)),
            _ =>
                Results.BadRequest(new ErrorResponse("Validation failed.", result.Errors))
        };
    }
}
