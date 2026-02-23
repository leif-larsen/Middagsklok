namespace Middagsklok.Api.Features.Recipes.Instructions;

internal static class RecipesInstructionsEndpoint
{
    // Maps the recipe instructions endpoint.
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/recipes/instructions", Handle)
            .WithName("GetRecipeInstructions");
    }

    // Handles the HTTP request and delegates to the use case.
    private static async Task<IResult> Handle(
        UseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(cancellationToken);

        return Results.Ok(result);
    }
}
