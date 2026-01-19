using Middagsklok.Contracts.Dishes;
using Middagsklok.Contracts.Dishes.Details;
using Middagsklok.Features.Dishes.Import;
using Middagsklok.Features.Dishes.List;
using Middagsklok.Features.Dishes.GetDishDetails;
using Middagsklok.Features.Dishes.UpdateDish;

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

        app.MapGet("/dishes/{id:guid}", async (Guid id, GetDishDetailsFeature feature, CancellationToken ct) =>
        {
            var query = new GetDishDetailsQuery(id);
            var dish = await feature.Execute(query, ct);

            if (dish is null)
            {
                return Results.NotFound();
            }

            var response = new DishDetailsResponse(
                Id: dish.Id.ToString(),
                Name: dish.Name,
                ActiveMinutes: dish.ActiveMinutes,
                TotalMinutes: dish.TotalMinutes,
                KidRating: dish.KidRating,
                FamilyRating: dish.FamilyRating,
                IsPescetarian: dish.IsPescetarian,
                HasOptionalMeatVariant: dish.HasOptionalMeatVariant,
                Ingredients: dish.Ingredients
                    .Select(i => new DishDetailsIngredientItem(
                        Name: i.Ingredient.Name,
                        Category: i.Ingredient.Category,
                        Amount: i.Amount,
                        Unit: i.Unit,
                        Optional: i.Optional
                    ))
                    .ToList()
            );

            return Results.Ok(response);
        })
        .Produces<DishDetailsResponse>()
        .Produces(StatusCodes.Status404NotFound);

        app.MapPut("/dishes/{id:guid}", async (
            Guid id,
            UpdateDishRequest request,
            UpdateDishFeature feature,
            CancellationToken ct) =>
        {
            try
            {
                var command = new Middagsklok.Features.Dishes.UpdateDish.UpdateDishCommand(
                    DishId: id,
                    Name: request.Name,
                    ActiveMinutes: request.ActiveMinutes,
                    TotalMinutes: request.TotalMinutes,
                    KidRating: request.KidRating,
                    FamilyRating: request.FamilyRating,
                    IsPescetarian: request.IsPescetarian,
                    HasOptionalMeatVariant: request.HasOptionalMeatVariant,
                    Ingredients: request.Ingredients.Select(i => 
                        new Middagsklok.Features.Dishes.UpdateDish.UpdateDishIngredientItem(
                            Name: i.Name,
                            Category: i.Category,
                            Amount: i.Amount,
                            Unit: i.Unit,
                            Optional: i.Optional
                        )).ToList()
                );

                var result = await feature.Execute(command, ct);

                if (result is null)
                {
                    return Results.NotFound();
                }

                var response = new UpdateDishResponse(
                    Id: result.Id.ToString(),
                    UpdatedAt: result.UpdatedAt.ToString("O"),
                    Warnings: result.Warnings.ToList()
                );

                return Results.Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .Produces<UpdateDishResponse>()
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

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
        })
        .Produces<BatchImportDishesResponse>();
    }
}
