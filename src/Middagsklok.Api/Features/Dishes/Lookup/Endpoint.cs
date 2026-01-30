namespace Middagsklok.Api.Features.Dishes.Lookup;

internal static class DishesLookupEndpoint
{
    // Maps the dish lookup endpoint.
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/dishes/lookup", Handle)
            .WithName("GetDishesLookup");
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
