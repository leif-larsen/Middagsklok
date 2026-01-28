namespace Middagsklok.Api.Features.Dishes.Import;

internal static class DishesImportEndpoint
{
    // Maps the import endpoint.
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/dishes/import", Handle)
            .WithName("ImportDishes");
    }

    // Handles the HTTP request and delegates to the use case.
    private static async Task<IResult> Handle(
        Request? request,
        UseCase useCase,
        CancellationToken cancellationToken)
    {
        var safeRequest = request ?? new Request(Array.Empty<DishInput>());
        var response = await useCase.Execute(safeRequest, cancellationToken);

        return Results.Ok(response);
    }
}
