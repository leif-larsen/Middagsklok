namespace Middagsklok.Api.Features.Ingredients.Delete;

internal static class IngredientsDeleteEndpoint
{
    // Maps the delete ingredient endpoint.
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/ingredients/{id}", Handle)
            .WithName("DeleteIngredient");
    }

    // Handles the HTTP request and delegates to the use case.
    private static async Task<IResult> Handle(
        string? id,
        UseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(id ?? string.Empty, cancellationToken);

        return result.Outcome switch
        {
            DeleteOutcome.Success => Results.Ok(),
            DeleteOutcome.NotFound =>
                Results.NotFound(new ErrorResponse("Ingredient not found.", result.Errors)),
            DeleteOutcome.InUse =>
                Results.BadRequest(new ErrorResponse("Ingredient is in use.", result.Errors)),
            _ =>
                Results.BadRequest(new ErrorResponse("Validation failed.", result.Errors))
        };
    }
}
