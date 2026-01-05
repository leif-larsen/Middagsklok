using Middagsklok.Domain;
using Middagsklok.Features.GetShoppingList;

namespace Middagsklok.Tests.Unit;

public class GetShoppingListFeatureTests
{
    private static Dish CreateDish(string name, List<DishIngredient> ingredients) =>
        new(Guid.NewGuid(), name, 
            ActiveMinutes: 10, TotalMinutes: 20, 
            KidRating: 3, FamilyRating: 4, 
            IsPescetarian: true, HasOptionalMeatVariant: false, 
            ingredients);

    [Fact]
    public void Execute_IgnoresOptionalIngredients()
    {
        // Arrange
        var tomato = new Ingredient(Guid.NewGuid(), "Tomat", "Grønnsaker", "stk");
        var basil = new Ingredient(Guid.NewGuid(), "Basilikum", "Urter", "stk");

        var dish = CreateDish("Salat",
        [
            new DishIngredient(tomato, 2, "stk", Optional: false),
            new DishIngredient(basil, 1, "stk", Optional: true)
        ]);

        var plan = new WeeklyPlan(
            Id: Guid.NewGuid(),
            WeekStartDate: new DateOnly(2026, 1, 5),
            CreatedAt: DateTimeOffset.UtcNow,
            Items: [new WeeklyPlanItem(DayIndex: 0, Dish: dish)]);

        // Act
        var result = GetShoppingListFeature.Execute(plan);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Tomat", result.Items[0].IngredientName);
    }

    [Fact]
    public void Execute_AggregatesAmountsByIngredientAndUnit()
    {
        // Arrange
        var pastaId = Guid.NewGuid();
        var pasta = new Ingredient(pastaId, "Pasta", "Tørrvarer", "g");

        var dish1 = CreateDish("Bolognese",
        [
            new DishIngredient(pasta, 400, "g", Optional: false)
        ]);

        var dish2 = CreateDish("Carbonara",
        [
            new DishIngredient(pasta, 300, "g", Optional: false)
        ]);

        var plan = new WeeklyPlan(
            Id: Guid.NewGuid(),
            WeekStartDate: new DateOnly(2026, 1, 5),
            CreatedAt: DateTimeOffset.UtcNow,
            Items:
            [
                new WeeklyPlanItem(DayIndex: 0, Dish: dish1),
                new WeeklyPlanItem(DayIndex: 1, Dish: dish2)
            ]);

        // Act
        var result = GetShoppingListFeature.Execute(plan);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Pasta", result.Items[0].IngredientName);
        Assert.Equal(700, result.Items[0].Amount);
        Assert.Equal("g", result.Items[0].Unit);
    }

    [Fact]
    public void Execute_SortsByCategoryThenIngredientName()
    {
        // Arrange
        var tomato = new Ingredient(Guid.NewGuid(), "Tomat", "Grønnsaker", "stk");
        var onion = new Ingredient(Guid.NewGuid(), "Løk", "Grønnsaker", "stk");
        var pasta = new Ingredient(Guid.NewGuid(), "Pasta", "Tørrvarer", "g");

        var dish = CreateDish("Middag",
        [
            new DishIngredient(pasta, 400, "g", Optional: false),
            new DishIngredient(tomato, 2, "stk", Optional: false),
            new DishIngredient(onion, 1, "stk", Optional: false)
        ]);

        var plan = new WeeklyPlan(
            Id: Guid.NewGuid(),
            WeekStartDate: new DateOnly(2026, 1, 5),
            CreatedAt: DateTimeOffset.UtcNow,
            Items: [new WeeklyPlanItem(DayIndex: 0, Dish: dish)]);

        // Act
        var result = GetShoppingListFeature.Execute(plan);

        // Assert
        Assert.Equal(3, result.Items.Count);
        Assert.Equal("Løk", result.Items[0].IngredientName);       // Grønnsaker
        Assert.Equal("Tomat", result.Items[1].IngredientName);     // Grønnsaker
        Assert.Equal("Pasta", result.Items[2].IngredientName);     // Tørrvarer
    }
}
