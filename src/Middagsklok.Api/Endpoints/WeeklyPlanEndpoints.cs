using Middagsklok.Features.WeeklyPlanning;

namespace Middagsklok.Api.Endpoints;

public static class WeeklyPlanEndpoints
{
    public static void MapWeeklyPlanEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/weekly-plan/{weekStartDate}", async (string weekStartDate, GetWeeklyPlanFeature feature, CancellationToken ct) =>
        {
            if (!DateOnly.TryParse(weekStartDate, out var date))
            {
                return Results.BadRequest(new { error = "Invalid date format. Use YYYY-MM-DD." });
            }

            var plan = await feature.Execute(date, ct);
            if (plan == null)
            {
                return Results.NotFound(new { error = "No plan found for the specified week." });
            }

            return Results.Ok(plan);
        })
        .WithName("GetWeeklyPlan");

        app.MapPost("/weekly-plan/generate", async (GenerateWeeklyPlanRequest request, GenerateWeeklyPlanFeature feature, CancellationToken ct) =>
        {
            try
            {
                var result = await feature.Execute(request, ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("GenerateWeeklyPlan");
    }
}
