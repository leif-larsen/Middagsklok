namespace Middagsklok.Api.Features.Ingredients.Metadata;

internal static class IngredientsMetadataEndpoint
{
    // Maps the ingredient metadata endpoint.
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/ingredients/metadata", Handle)
            .WithName("GetIngredientsMetadata");
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
