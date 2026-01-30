namespace Middagsklok.Api.Features.ShoppingList.ByStartDate;

internal static class ShoppingListByStartDateEndpoint
{
    // Maps the shopping list retrieval endpoint.
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/shopping-list/{startDate}", Handle)
            .WithName("GetShoppingList");
    }

    // Handles the HTTP request and delegates to the use case.
    private static async Task<IResult> Handle(
        string? startDate,
        UseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(startDate, cancellationToken);

        return result.Outcome switch
        {
            FetchOutcome.Success when result.ShoppingList is not null =>
                Results.Ok(result.ShoppingList),
            FetchOutcome.NotFound =>
                Results.NotFound(new ErrorResponse("Shopping list not found.", result.Errors)),
            _ =>
                Results.BadRequest(new ErrorResponse("Validation failed.", result.Errors))
        };
    }
}
