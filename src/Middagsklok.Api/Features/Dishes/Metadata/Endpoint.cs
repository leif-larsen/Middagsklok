namespace Middagsklok.Api.Features.Dishes.Metadata;

internal static class DishesMetadataEndpoint
{
    // Maps the dishes metadata endpoint.
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/dishes/metadata", Handle)
            .WithName("GetDishesMetadata");
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
