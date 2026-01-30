namespace Middagsklok.Api.Features.WeeklyPlans.Generate;

internal static class WeeklyPlansGenerateEndpoint
{
    // Maps the weekly plan generation endpoint.
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/weekly-plans/generate/{startDate}", Handle)
            .WithName("GenerateWeeklyPlan");
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
            GenerateOutcome.Success when result.Plan is not null =>
                Results.Ok(result.Plan),
            _ =>
                Results.BadRequest(new ErrorResponse("Validation failed.", result.Errors))
        };
    }
}
