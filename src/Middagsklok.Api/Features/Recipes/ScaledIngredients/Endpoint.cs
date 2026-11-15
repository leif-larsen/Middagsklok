namespace Middagsklok.Api.Features.Recipes.ScaledIngredients;

internal static class RecipesScaledIngredientsEndpoint
{
    // Maps the endpoint returning a dish's ingredients scaled to a number of servings.
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/recipes/{dishId}/ingredients", Handle)
            .WithName("GetRecipeScaledIngredients");
    }

    // Handles the HTTP request and delegates to the use case.
    private static async Task<IResult> Handle(
        string? dishId,
        int? servings,
        UseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(dishId, servings, cancellationToken);

        return result.Outcome switch
        {
            FetchOutcome.Success when result.Recipe is not null =>
                Results.Ok(result.Recipe),
            FetchOutcome.NotFound =>
                Results.NotFound(new ErrorResponse("Dish not found.", result.Errors)),
            _ =>
                Results.BadRequest(new ErrorResponse("Validation failed.", result.Errors))
        };
    }
}
