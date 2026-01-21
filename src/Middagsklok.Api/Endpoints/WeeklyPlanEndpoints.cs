using Middagsklok.Contracts.WeeklyPlans;
using Middagsklok.Contracts.WeeklyPlans.Edit;
using Middagsklok.Features.WeeklyPlans.Get;
using Middagsklok.Features.WeeklyPlans.Generate;
using Middagsklok.Features.WeeklyPlans.Edit;
using Middagsklok.Features.WeeklyPlans.Suggest;
using Middagsklok.Features.WeeklyPlans.Save;

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
        })
        .Produces<WeeklyPlanResponse>()
        .Produces(404);

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
        })
        .Produces<GenerateWeeklyPlanResponse>();

        app.MapPost("/weekly-plan/suggest", async (
            Middagsklok.Contracts.WeeklyPlans.SuggestWeeklyPlanRequest request,
            SuggestWeeklyPlanFeature feature,
            CancellationToken ct) =>
        {
            if (!DateOnly.TryParseExact(request.WeekStartDate, "yyyy-MM-dd", out var date))
            {
                return Results.BadRequest(new { error = "Invalid date format. Use yyyy-MM-dd" });
            }

            try
            {
                var featureRequest = new Middagsklok.Features.WeeklyPlans.Suggest.SuggestWeeklyPlanRequest(
                    WeekStartDate: date
                );

                var result = await feature.Execute(featureRequest, ct);

                var items = result.Plan.Items.Select(item => new WeeklyPlanItemDto(
                    DayIndex: item.DayIndex,
                    Dish: new WeeklyPlanDishDto(
                        Id: item.Dish.Id.ToString(),
                        Name: item.Dish.Name,
                        ActiveMinutes: item.Dish.ActiveMinutes,
                        TotalMinutes: item.Dish.TotalMinutes
                    )
                )).ToList();

                var explanations = result.ExplanationsByDay.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new PlannedDishExplanationDto(
                        DishId: kvp.Value.DishId.ToString(),
                        Reasons: kvp.Value.Reasons.ToList()
                    )
                );

                var violations = result.Violations.Select(v => new RuleViolationDto(
                    v.RuleCode,
                    v.Message,
                    v.DayIndices.ToList()
                )).ToList();

                var response = new Middagsklok.Contracts.WeeklyPlans.SuggestWeeklyPlanResponse(
                    WeekStartDate: result.Plan.WeekStartDate.ToString("yyyy-MM-dd"),
                    Items: items,
                    ExplanationsByDay: explanations,
                    Violations: violations
                );

                return Results.Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .Produces<Middagsklok.Contracts.WeeklyPlans.SuggestWeeklyPlanResponse>()
        .Produces(400);

        app.MapPost("/weekly-plan/save", async (
            Middagsklok.Contracts.WeeklyPlans.SaveWeeklyPlanRequest request,
            SaveWeeklyPlanFeature feature,
            CancellationToken ct) =>
        {
            if (!DateOnly.TryParseExact(request.WeekStartDate, "yyyy-MM-dd", out var date))
            {
                return Results.BadRequest(new { error = "Invalid date format. Use yyyy-MM-dd" });
            }

            try
            {
                var items = request.Items.Select(i =>
                    new Middagsklok.Features.WeeklyPlans.Save.SaveWeeklyPlanItemRequest(
                        i.DayIndex,
                        Guid.Parse(i.DishId)
                    )).ToList();

                var featureRequest = new Middagsklok.Features.WeeklyPlans.Save.SaveWeeklyPlanRequest(
                    WeekStartDate: date,
                    Items: items
                );

                var result = await feature.Execute(featureRequest, ct);

                var violations = result.Violations.Select(v => new RuleViolationDto(
                    v.RuleCode,
                    v.Message,
                    v.DayIndices.ToList()
                )).ToList();

                var response = new Middagsklok.Contracts.WeeklyPlans.SaveWeeklyPlanResponse(
                    WeekStartDate: result.Plan.WeekStartDate.ToString("yyyy-MM-dd"),
                    Status: result.Status,
                    Violations: violations
                );

                if (result.Status == "validation_failed")
                {
                    return Results.BadRequest(response);
                }

                return Results.Ok(response);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .Produces<Middagsklok.Contracts.WeeklyPlans.SaveWeeklyPlanResponse>()
        .Produces(400);

        app.MapPut("/weekly-plan/{weekStartDate}", async (
            string weekStartDate,
            UpdateWeeklyPlanRequest request,
            EditWeeklyPlanFeature feature,
            CancellationToken ct) =>
        {
            if (!DateOnly.TryParseExact(weekStartDate, "yyyy-MM-dd", out var date))
            {
                return Results.BadRequest(new { error = "Invalid date format. Use yyyy-MM-dd" });
            }

            // Map from contract DTO to feature request
            var items = request.Items.Select(i => 
                new EditWeeklyPlanItemRequest(i.DayIndex, Guid.Parse(i.DishId))).ToList();
            var featureRequest = new EditWeeklyPlanRequest(date, items);

            try
            {
                var (plan, violations) = await feature.Execute(featureRequest, ct);

                if (violations.Any())
                {
                    var violationDtos = violations.Select(v => new RuleViolationDto(
                        v.RuleCode,
                        v.Message,
                        v.DayIndices.ToList()
                    )).ToList();

                    return Results.BadRequest(new UpdateWeeklyPlanResponse(
                        WeekStartDate: date.ToString("yyyy-MM-dd"),
                        Status: "validation_failed",
                        Violations: violationDtos
                    ));
                }

                var response = new UpdateWeeklyPlanResponse(
                    WeekStartDate: date.ToString("yyyy-MM-dd"),
                    Status: "updated",
                    Violations: Array.Empty<RuleViolationDto>()
                );

                return Results.Ok(response);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .Produces<UpdateWeeklyPlanResponse>()
        .Produces(400);
    }
}
