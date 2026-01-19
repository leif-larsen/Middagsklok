using Middagsklok.Domain;

namespace Middagsklok.Features.ShoppingList.GenerateForWeek;

public static class GetShoppingListFeature
{
    public static ShoppingList Execute(WeeklyPlan plan)
    {
        var aggregated = plan.Items
            .SelectMany(item => item.Dish.Ingredients)
            .Where(di => !di.Optional)
            .GroupBy(di => (di.Ingredient.Id, di.Unit))
            .Select(group =>
            {
                var first = group.First();
                return new ShoppingListItem(
                    IngredientName: first.Ingredient.Name,
                    Category: first.Ingredient.Category,
                    Amount: group.Sum(di => di.Amount),
                    Unit: first.Unit);
            })
            .OrderBy(item => item.Category)
            .ThenBy(item => item.IngredientName)
            .ToList();

        return new ShoppingList(aggregated);
    }
}
