namespace Middagsklok.Api.Features.Ingredients.OdaMappings.Overview;

internal static class IngredientsOdaMappingsOverviewEndpoint
{
    // Maps the Oda mappings overview endpoint.
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/ingredients/oda-mappings", Handle)
            .WithName("GetIngredientOdaMappings");
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
