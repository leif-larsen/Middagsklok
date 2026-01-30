namespace Middagsklok.Api.Features.WeeklyPlans.Available;

internal static class WeeklyPlansAvailableEndpoint
{
    // Maps the weekly plan availability endpoint.
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/weekly-plans", Handle)
            .WithName("GetWeeklyPlans");
    }

    // Handles the HTTP request and delegates to the use case.
    private static async Task<IResult> Handle(
        UseCase useCase,
        CancellationToken cancellationToken)
    {
        var response = await useCase.Execute(cancellationToken);

        return Results.Ok(response);
    }
}
