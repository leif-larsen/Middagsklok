using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.Dish;
using Middagsklok.Api.Domain.Ingredient;
using Middagsklok.Api.Domain.Settings;
using Middagsklok.Api.Domain.WeeklyPlan;
using Middagsklok.Api.Features.ShoppingList.ByStartDate;
using TUnit.Assertions;
using TUnit.Core;
using DishEntity = Middagsklok.Api.Domain.Dish.Dish;
using IngredientEntity = Middagsklok.Api.Domain.Ingredient.Ingredient;

namespace Middagsklok.Tests.Features.ShoppingList.ByStartDate;

public sealed class OdaProductTests
{
    private static readonly DateOnly StartDate = new(2026, 9, 18);

    // Creates an in-memory AppDbContext for test isolation.
    private static AppDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var context = new AppDbContext(options);
        return context;
    }

    // Seeds a one-dish week for three servings and returns the single shopping item.
    private static async Task<ShoppingItem> RunSingleIngredientWeek(
        AppDbContext context,
        IngredientEntity ingredient,
        double quantityPerServing,
        Unit unit)
    {
        var dishIngredient = new DishIngredient(ingredient.Id, quantityPerServing, unit, null, 1, IngredientScaling.PerServing);
        var dish = new DishEntity("Testrett", DishType.Other, 10, 10, 4, null, false, false, false, [dishIngredient]);

        var days = Enumerable.Range(0, 7)
            .Select(offset => offset == 0
                ? new PlannedDay(StartDate, new DishSelection(DishSelectionType.Dish, dish.Id))
                : new PlannedDay(StartDate.AddDays(offset), new DishSelection(DishSelectionType.Empty, null)))
            .ToArray();

        context.Ingredients.Add(ingredient);
        context.Dishes.Add(dish);
        context.WeeklyPlans.Add(new WeeklyPlan(StartDate, days));
        context.PlanningSettings.Add(new PlanningSettings(DayOfWeek.Friday, 2, 14, 3));
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new UseCase(context).Execute("2026-09-18", CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(FetchOutcome.Success);

        var item = result.ShoppingList!.Categories.SelectMany(category => category.Items).Single();

        return item;
    }

    // Verifies that an unmapped ingredient reports its status and carries no product block.
    [Test]
    public async Task UnmappedIngredientHasNoProductBlock()
    {
        await using var context = CreateContext(Guid.NewGuid().ToString("N"));
        var ingredient = new IngredientEntity("Dill", IngredientCategory.Produce, Unit.G);

        var item = await RunSingleIngredientWeek(context, ingredient, 10, Unit.G);

        await Assert.That(item.OdaStatus).IsEqualTo("Unmapped");
        await Assert.That(item.OdaProduct).IsNull();
    }

    // Verifies that a mapped ingredient exposes the product and the package count for the scaled amount.
    [Test]
    public async Task MappedIngredientSuggestsPackCountForScaledAmount()
    {
        await using var context = CreateContext(Guid.NewGuid().ToString("N"));
        var ingredient = new IngredientEntity("Kyllingfilet", IngredientCategory.Poultry, Unit.G);
        ingredient.SetOdaMapping(OdaProductMapping.ForProduct(66899, "Ytterøy Kyllingfilet", 400, Unit.G, true));

        var item = await RunSingleIngredientWeek(context, ingredient, 150, Unit.G);

        await Assert.That(item.Amount).IsEqualTo(450);
        await Assert.That(item.OdaStatus).IsEqualTo("Mapped");
        await Assert.That(item.OdaProduct).IsNotNull();
        await Assert.That(item.OdaProduct!.ProductId).IsEqualTo(66899);
        await Assert.That(item.OdaProduct.SuggestedPackCount).IsEqualTo(2);
        await Assert.That(item.OdaProduct.Confirmed).IsTrue();
    }

    // Verifies that an ingredient Oda does not stock is flagged without a product block.
    [Test]
    public async Task NotAvailableIngredientIsFlagged()
    {
        await using var context = CreateContext(Guid.NewGuid().ToString("N"));
        var ingredient = new IngredientEntity("Tortelloni", IngredientCategory.PastaAndGrains, Unit.Pack);
        ingredient.SetOdaMapping(OdaProductMapping.NotAvailable());

        var item = await RunSingleIngredientWeek(context, ingredient, 1, Unit.Pack);

        await Assert.That(item.OdaStatus).IsEqualTo("NotAvailable");
        await Assert.That(item.OdaProduct).IsNull();
    }
}
