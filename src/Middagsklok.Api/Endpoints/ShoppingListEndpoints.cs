using Middagsklok.Contracts.ShoppingList;
using Middagsklok.Features.ShoppingList.GenerateForWeek;

namespace Middagsklok.Api.Endpoints;

public static class ShoppingListEndpoints
{
    public static void MapShoppingListEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/shopping-list/{weekStartDate}", async (
            string weekStartDate,
            GetShoppingListForWeekFeature feature,
            CancellationToken ct) =>
        {
            if (!DateOnly.TryParseExact(weekStartDate, "yyyy-MM-dd", out var date))
            {
                return Results.BadRequest(new { error = "Invalid date format. Use yyyy-MM-dd" });
            }

            var shoppingList = await feature.Execute(date, ct);
            if (shoppingList == null)
            {
                return Results.NotFound();
            }

            // Map from domain to contract DTO
            var response = new ShoppingListResponse(
                WeekStartDate: date.ToString("yyyy-MM-dd"),
                Items: shoppingList.Items.Select(item => new ShoppingListItemDto(
                    IngredientName: item.IngredientName,
                    Category: item.Category,
                    Amount: item.Amount,
                    Unit: item.Unit
                )).ToList()
            );

            return Results.Ok(response);
        })
        .Produces<ShoppingListResponse>()
        .Produces(404)
        .Produces(400);
    }
}
