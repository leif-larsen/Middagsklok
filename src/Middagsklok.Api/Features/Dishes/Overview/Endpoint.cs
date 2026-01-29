namespace Middagsklok.Api.Features.Dishes.Overview;

internal static class DishesOverviewEndpoint
{
    // Maps the dishes overview endpoint.
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/dishes", Handle)
            .WithName("GetDishes");
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
