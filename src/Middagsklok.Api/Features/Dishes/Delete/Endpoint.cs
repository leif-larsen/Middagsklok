namespace Middagsklok.Api.Features.Dishes.Delete;

internal static class DishesDeleteEndpoint
{
    // Maps the delete dish endpoint.
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/dishes/{id}", Handle)
            .WithName("DeleteDish");
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
                Results.NotFound(new ErrorResponse("Dish not found.", result.Errors)),
            _ =>
                Results.BadRequest(new ErrorResponse("Validation failed.", result.Errors))
        };
    }
}
