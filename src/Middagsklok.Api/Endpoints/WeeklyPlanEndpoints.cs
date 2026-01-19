using Middagsklok.Features.WeeklyPlanning;

namespace Middagsklok.Api.Endpoints;

public static class WeeklyPlanEndpoints
{
    public static void MapWeeklyPlanEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/weekly-plan/{weekStartDate}", async (
            string weekStartDate,
            GetWeeklyPlanFeature feature,
            CancellationToken ct) =>
        {
            if (!DateOnly.TryParseExact(weekStartDate, "yyyy-MM-dd", out var date))
            {
                return Results.BadRequest(new { error = "Invalid date format. Use yyyy-MM-dd" });
            }

            var plan = await feature.Execute(date, ct);
            return plan != null ? Results.Ok(plan) : Results.NotFound();
        });

        app.MapPost("/weekly-plan/generate", async (
            GenerateWeeklyPlanRequest request,
            GenerateWeeklyPlanFeature feature,
            CancellationToken ct) =>
        {
            var result = await feature.Execute(request, ct);
            return Results.Ok(result);
        });
    }
}
