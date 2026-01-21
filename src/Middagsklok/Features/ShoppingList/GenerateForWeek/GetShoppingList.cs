using Middagsklok.Domain;
using Middagsklok.Features.Shared;

namespace Middagsklok.Features.ShoppingList.GenerateForWeek;

public static class GetShoppingListFeature
{
    public static ShoppingList Execute(WeeklyPlan plan)
    {
        var generator = new ShoppingListGenerator();
        return generator.Generate(plan);
    }
}
