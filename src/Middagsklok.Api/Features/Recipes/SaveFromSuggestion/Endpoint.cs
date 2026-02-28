namespace Middagsklok.Api.Features.Recipes.SaveFromSuggestion;

internal static class RecipesSaveFromSuggestionEndpoint
{
    // Maps the save recipe from AI suggestion endpoint.
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/recipes/save-from-suggestion", Handle)
            .WithName("SaveRecipeFromSuggestion");
    }

    // Handles the HTTP request and delegates to the use case.
    private static async Task<IResult> Handle(
        Request? request,
        UseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(request, cancellationToken);

        return result.Outcome switch
        {
            SaveOutcome.Success when result.Dish is not null =>
                Results.Ok(result.Dish),
            SaveOutcome.Conflict =>
                Results.Conflict(new ErrorResponse("Dish already exists.", result.Errors)),
            SaveOutcome.Unavailable =>
                Results.Json(
                    new ErrorResponse("AI provider is unavailable.", result.Errors),
                    statusCode: StatusCodes.Status503ServiceUnavailable),
            _ =>
                Results.BadRequest(new ErrorResponse("Validation failed.", result.Errors))
        };
    }
}
