using Middagsklok.Features.BatchImportDishes;
using Middagsklok.Features.GetDishes;

namespace Middagsklok.Api.Endpoints;

public static class DishEndpoints
{
    public static void MapDishEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/dishes", async (GetDishesFeature feature, CancellationToken ct) =>
        {
            var dishes = await feature.Execute(ct);
            return Results.Ok(dishes.Select(d => new
            {
                id = d.Id,
                name = d.Name,
                activeMinutes = d.ActiveMinutes,
                totalMinutes = d.TotalMinutes
            }));
        })
        .WithName("GetDishes");

        app.MapPost("/dishes/import", async (BatchImportDishesCommand command, BatchImportDishesFeature feature, CancellationToken ct) =>
        {
            try
            {
                var result = await feature.Execute(command, ct);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("ImportDishes");
    }
}
