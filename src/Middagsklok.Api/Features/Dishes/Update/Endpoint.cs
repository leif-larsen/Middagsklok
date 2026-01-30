namespace Middagsklok.Api.Features.Dishes.Update;

internal static class DishesUpdateEndpoint
{
    // Maps the update dish endpoint.
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/dishes/{id}", Handle)
            .WithName("UpdateDish");
    }

    // Handles the HTTP request and delegates to the use case.
    private static async Task<IResult> Handle(
        string? id,
        Request? request,
        UseCase useCase,
        CancellationToken cancellationToken)
    {
        var safeRequest = request ?? new Request(null, null, 0, 0, 0, null, Array.Empty<IngredientInput>());
        var result = await useCase.Execute(id ?? string.Empty, safeRequest, cancellationToken);

        return result.Outcome switch
        {
            UpdateOutcome.Success when result.Dish is not null =>
                Results.Ok(result.Dish),
            UpdateOutcome.Conflict =>
                Results.Conflict(new ErrorResponse("Dish name already exists.", result.Errors)),
            UpdateOutcome.NotFound =>
                Results.NotFound(new ErrorResponse("Dish not found.", result.Errors)),
            _ =>
                Results.BadRequest(new ErrorResponse("Validation failed.", result.Errors))
        };
    }
}
