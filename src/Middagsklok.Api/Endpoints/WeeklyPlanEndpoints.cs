using Middagsklok.Contracts.WeeklyPlans;
using Middagsklok.Features.WeeklyPlans.Get;
using Middagsklok.Features.WeeklyPlans.Generate;

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
            if (plan == null)
            {
                return Results.NotFound();
            }
            
            // Map from domain to contract DTO
            var response = new WeeklyPlanResponse(
                WeekStartDate: plan.WeekStartDate.ToString("yyyy-MM-dd"),
                Items: plan.Items.Select(item => new WeeklyPlanItemDto(
                    DayIndex: item.DayIndex,
                    Dish: new WeeklyPlanDishDto(
                        Id: item.Dish.Id.ToString(),
                        Name: item.Dish.Name,
                        ActiveMinutes: item.Dish.ActiveMinutes,
                        TotalMinutes: item.Dish.TotalMinutes
                    )
                )).ToList()
            );
            
            return Results.Ok(response);
        });

        app.MapPost("/weekly-plan/generate", async (
            Middagsklok.Contracts.WeeklyPlans.GenerateWeeklyPlanRequest request,
            GenerateWeeklyPlanFeature feature,
            CancellationToken ct) =>
        {
            if (!DateOnly.TryParseExact(request.WeekStartDate, "yyyy-MM-dd", out var date))
            {
                return Results.BadRequest(new { error = "Invalid date format. Use yyyy-MM-dd" });
            }
            
            // Map from contract DTO to feature request
            var featureRequest = new Middagsklok.Features.WeeklyPlans.Generate.GenerateWeeklyPlanRequest(
                WeekStartDate: date
            );
            
            var result = await feature.Execute(featureRequest, ct);
            
            // Map from feature result to contract DTO
            var planResponse = new WeeklyPlanResponse(
                WeekStartDate: result.Plan.WeekStartDate.ToString("yyyy-MM-dd"),
                Items: result.Plan.Items.Select(item => new WeeklyPlanItemDto(
                    DayIndex: item.DayIndex,
                    Dish: new WeeklyPlanDishDto(
                        Id: item.Dish.Id.ToString(),
                        Name: item.Dish.Name,
                        ActiveMinutes: item.Dish.ActiveMinutes,
                        TotalMinutes: item.Dish.TotalMinutes
                    )
                )).ToList()
            );
            
            var explanations = result.ExplanationsByDay.ToDictionary(
                kvp => kvp.Key,
                kvp => new PlannedDishExplanationDto(
                    DishId: kvp.Value.DishId.ToString(),
                    Reasons: kvp.Value.Reasons.ToList()
                )
            );
            
            var response = new GenerateWeeklyPlanResponse(
                Plan: planResponse,
                ExplanationsByDay: explanations
            );
            
            return Results.Ok(response);
        });
    }
}
