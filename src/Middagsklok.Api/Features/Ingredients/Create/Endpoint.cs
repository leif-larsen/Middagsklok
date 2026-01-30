namespace Middagsklok.Api.Features.Ingredients.Create;

internal static class IngredientsCreateEndpoint
{
    // Maps the create ingredient endpoint.
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/ingredients", Handle)
            .WithName("CreateIngredient");
    }

    // Handles the HTTP request and delegates to the use case.
    private static async Task<IResult> Handle(
        Request? request,
        UseCase useCase,
        CancellationToken cancellationToken)
    {
        var safeRequest = request ?? new Request(null, null, null);
        var result = await useCase.Execute(safeRequest, cancellationToken);

        if (!result.IsValid || result.Ingredient is null)
        {
            var errorResponse = new ErrorResponse("Validation failed.", result.Errors);
            return Results.BadRequest(errorResponse);
        }

        return Results.Ok(result.Ingredient);
    }
}
