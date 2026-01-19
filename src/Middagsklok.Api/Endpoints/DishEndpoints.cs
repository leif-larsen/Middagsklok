using Middagsklok.Contracts.Dishes;
using Middagsklok.Features.Dishes.Import;
using Middagsklok.Features.Dishes.List;

namespace Middagsklok.Api.Endpoints;

public static class DishEndpoints
{
    public static void MapDishEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/dishes", async (GetDishesFeature feature, CancellationToken ct) =>
        {
            var dishes = await feature.Execute(ct);
            var items = dishes.Select(d => new DishListItem(
                Id: d.Id.ToString(),
                Name: d.Name,
                ActiveMinutes: d.ActiveMinutes,
                TotalMinutes: d.TotalMinutes
            )).ToList();
            
            return new DishListResponse(items);
        });

        app.MapPost("/dishes/import", async (
            BatchImportDishesRequest request,
            BatchImportDishesFeature feature,
            CancellationToken ct) =>
        {
            // Map from contract DTO to feature command
            var command = new BatchImportDishesCommand(
                request.Dishes.Select(d => new AddDishCommand(
                    Name: d.Name,
                    ActiveMinutes: d.ActiveMinutes,
                    TotalMinutes: d.TotalMinutes,
                    KidRating: d.KidRating,
                    FamilyRating: d.FamilyRating,
                    IsPescetarian: d.IsPescetarian,
                    HasOptionalMeatVariant: d.HasOptionalMeatVariant,
                    Tags: d.Tags,
                    Ingredients: d.Ingredients.Select(i => new AddDishIngredientItem(
                        Name: i.Name,
                        Category: i.Category,
                        Amount: i.Amount,
                        Unit: i.Unit,
                        Optional: i.Optional
                    )).ToList()
                )).ToList()
            );
            
            var result = await feature.Execute(command, ct);
            
            // Map from feature result to contract DTO
            var response = new BatchImportDishesResponse(
                Total: result.Total,
                Created: result.Created,
                Skipped: result.Skipped,
                Failed: result.Failed,
                Results: result.Results.Select(r => new BatchImportDishResultItem(
                    Name: r.Name,
                    Status: r.Status,
                    DishId: r.DishId?.ToString(),
                    Error: r.Error
                )).ToList()
            );
            
            return Results.Ok(response);
        });
    }
}
