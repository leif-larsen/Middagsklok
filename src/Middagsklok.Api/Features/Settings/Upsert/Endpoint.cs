namespace Middagsklok.Api.Features.Settings.Upsert;

internal static class PlanningSettingsUpsertEndpoint
{
    // Maps the planning settings upsert endpoint.
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/planning-settings", Handle)
            .WithName("UpsertPlanningSettings");
    }

    // Handles the HTTP request and delegates to the use case.
    private static async Task<IResult> Handle(
        Request? request,
        UseCase useCase,
        CancellationToken cancellationToken)
    {
        var safeRequest = request ?? new Request(null);
        var result = await useCase.Execute(safeRequest, cancellationToken);

        return result.Outcome switch
        {
            UpsertOutcome.Success when result.Settings is not null =>
                Results.Ok(result.Settings),
            _ =>
                Results.BadRequest(new ErrorResponse("Validation failed.", result.Errors))
        };
    }
}
