namespace Middagsklok.Api.Features.Dishes.Create;

internal static class DishesCreateEndpoint
{
    // Maps the create dish endpoint.
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/dishes", Handle)
            .WithName("CreateDish");
    }

    // Handles the HTTP request and delegates to the use case.
    private static async Task<IResult> Handle(
        Request? request,
        UseCase useCase,
        CancellationToken cancellationToken)
    {
        var safeRequest = request ?? new Request(null, null, 0, 0, 0, null, Array.Empty<IngredientInput>());
        var result = await useCase.Execute(safeRequest, cancellationToken);

        return result.Outcome switch
        {
            CreateOutcome.Success when result.Dish is not null =>
                Results.Ok(result.Dish),
            CreateOutcome.Conflict =>
                Results.Conflict(new ErrorResponse("Dish name already exists.", result.Errors)),
            _ =>
                Results.BadRequest(new ErrorResponse("Validation failed.", result.Errors))
        };
    }
}
