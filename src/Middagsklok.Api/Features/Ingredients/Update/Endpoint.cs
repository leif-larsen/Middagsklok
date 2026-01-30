namespace Middagsklok.Api.Features.Ingredients.Update;

internal static class IngredientsUpdateEndpoint
{
    // Maps the update ingredient endpoint.
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/ingredients/{id}", Handle)
            .WithName("UpdateIngredient");
    }

    // Handles the HTTP request and delegates to the use case.
    private static async Task<IResult> Handle(
        string? id,
        Request? request,
        UseCase useCase,
        CancellationToken cancellationToken)
    {
        var safeRequest = request ?? new Request(null, null, null);
        var result = await useCase.Execute(id ?? string.Empty, safeRequest, cancellationToken);

        return result.Outcome switch
        {
            UpdateOutcome.Success when result.Ingredient is not null =>
                Results.Ok(result.Ingredient),
            UpdateOutcome.Conflict =>
                Results.Conflict(new ErrorResponse("Ingredient name already exists.", result.Errors)),
            UpdateOutcome.NotFound =>
                Results.NotFound(new ErrorResponse("Ingredient not found.", result.Errors)),
            _ =>
                Results.BadRequest(new ErrorResponse("Validation failed.", result.Errors))
        };
    }
}
