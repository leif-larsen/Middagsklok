namespace Middagsklok.Api.Features.Dishes.Retire;

internal static class DishesRetireEndpoint
{
    // Maps the retire and un-retire dish endpoints.
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/dishes/{id}/retire", (string? id, UseCase useCase, CancellationToken cancellationToken) =>
            Handle(id ?? string.Empty, true, useCase, cancellationToken))
            .WithName("RetireDish");

        app.MapDelete("/dishes/{id}/retire", (string? id, UseCase useCase, CancellationToken cancellationToken) =>
            Handle(id ?? string.Empty, false, useCase, cancellationToken))
            .WithName("UnretireDish");
    }

    // Handles the HTTP request and delegates to the use case.
    private static async Task<IResult> Handle(
        string id,
        bool retire,
        UseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(id, retire, cancellationToken);

        return result.Outcome switch
        {
            RetireOutcome.Success => Results.Ok(),
            RetireOutcome.NotFound =>
                Results.NotFound(new ErrorResponse("Dish not found.", result.Errors)),
            _ =>
                Results.BadRequest(new ErrorResponse("Validation failed.", result.Errors))
        };
    }
}

internal sealed record ErrorResponse(
    string Message,
    IReadOnlyList<ValidationError> Errors);
