namespace Middagsklok.Api.Features.Ingredients.OdaMappings.Upsert;

internal static class IngredientsOdaMappingUpsertEndpoint
{
    // Maps the set-or-replace Oda mapping endpoint.
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/ingredients/{id}/oda-mapping", Handle)
            .WithName("UpsertIngredientOdaMapping");
    }

    // Handles the HTTP request and delegates to the use case.
    private static async Task<IResult> Handle(
        string? id,
        Request? request,
        UseCase useCase,
        CancellationToken cancellationToken)
    {
        var safeRequest = request ?? new Request(null, null, null, null);
        var result = await useCase.Execute(id ?? string.Empty, safeRequest, cancellationToken);

        return result.Outcome switch
        {
            UpsertOutcome.Success when result.Mapping is not null =>
                Results.Ok(result.Mapping),
            UpsertOutcome.NotFound =>
                Results.NotFound(new ErrorResponse("Ingredient not found.", result.Errors)),
            _ =>
                Results.BadRequest(new ErrorResponse("Validation failed.", result.Errors))
        };
    }
}
