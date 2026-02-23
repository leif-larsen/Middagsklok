namespace Middagsklok.Api.Features.Recipes.Suggestions;

internal static class RecipesSuggestionsEndpoint
{
    // Maps the recipe suggestions endpoint.
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/recipes/suggestions", Handle)
            .WithName("GetRecipeSuggestions");
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
            SuggestionsOutcome.Success when result.Response is not null =>
                Results.Ok(result.Response),
            SuggestionsOutcome.Unavailable =>
                Results.Json(
                    new ErrorResponse("AI provider is unavailable.", result.Errors),
                    statusCode: StatusCodes.Status503ServiceUnavailable),
            _ =>
                Results.BadRequest(new ErrorResponse("Validation failed.", result.Errors))
        };
    }
}
