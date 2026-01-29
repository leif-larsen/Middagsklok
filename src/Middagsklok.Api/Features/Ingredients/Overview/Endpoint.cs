namespace Middagsklok.Api.Features.Ingredients.Overview;

internal static class IngredientsOverviewEndpoint
{
    // Maps the ingredients overview endpoint.
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/ingredients", Handle)
            .WithName("GetIngredients");
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
