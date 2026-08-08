using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.Dish;
using Middagsklok.Api.Domain.DishHistory;
using Middagsklok.Api.Domain.Ingredient;
using Middagsklok.Api.Domain.Settings;
using Middagsklok.Api.Domain.WeeklyPlan;
using Middagsklok.Api.Features.ShoppingList.ByStartDate;
using TUnit.Assertions;
using TUnit.Core;
using DishEntity = Middagsklok.Api.Domain.Dish.Dish;
using IngredientEntity = Middagsklok.Api.Domain.Ingredient.Ingredient;

namespace Middagsklok.Tests.Features.ShoppingList.ByStartDate;

public sealed class UseCaseTests
{
    private static readonly DateOnly StartDate = new(2026, 2, 2);

    // Creates an in-memory AppDbContext for test isolation.
    private static AppDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var context = new AppDbContext(options);
        return context;
    }

    // Creates a weekly plan with empty selections for seven days.
    private static WeeklyPlan CreateEmptyWeeklyPlan(DateOnly startDate)
    {
        var days = Enumerable.Range(0, 7)
            .Select(offset => new PlannedDay(
                startDate.AddDays(offset),
                new DishSelection(DishSelectionType.Empty, null)))
            .ToArray();

        var plan = new WeeklyPlan(startDate, days);
        return plan;
    }

    // Creates a weekly plan where the given dishes occupy the leading days.
    private static WeeklyPlan CreateWeeklyPlan(
        DateOnly startDate,
        params (Guid DishId, int? Servings)[] selections)
    {
        var days = Enumerable.Range(0, 7)
            .Select(offset => offset < selections.Length
                ? new PlannedDay(
                    startDate.AddDays(offset),
                    new DishSelection(DishSelectionType.Dish, selections[offset].DishId),
                    selections[offset].Servings)
                : new PlannedDay(
                    startDate.AddDays(offset),
                    new DishSelection(DishSelectionType.Empty, null)))
            .ToArray();

        var plan = new WeeklyPlan(startDate, days);
        return plan;
    }

    // Builds a single-ingredient dish using the given scaling behaviour.
    private static DishEntity CreateDish(
        string name,
        Guid ingredientId,
        double quantity,
        IngredientScaling scaling,
        int? personCount = null,
        Unit unit = Unit.G)
    {
        var dishIngredient = new DishIngredient(
            ingredientId,
            quantity,
            unit,
            null,
            1,
            scaling,
            personCount);

        var dish = new DishEntity(
            name,
            DishType.Other,
            10,
            10,
            4,
            null,
            false,
            false,
            false,
            [dishIngredient],
            null);

        return dish;
    }

    // Seeds an ingredient, one dish and planning settings, then runs the use case.
    private static async Task<Response> RunAsync(
        AppDbContext context,
        WeeklyPlan plan,
        IReadOnlyList<DishEntity> dishes,
        IngredientEntity ingredient,
        int householdSize)
    {
        context.Ingredients.Add(ingredient);
        context.Dishes.AddRange(dishes);
        context.WeeklyPlans.Add(plan);
        context.PlanningSettings.Add(new PlanningSettings(DayOfWeek.Monday, 2, 14, householdSize));
        await context.SaveChangesAsync(CancellationToken.None);

        var useCase = new UseCase(context);
        var result = await useCase.Execute(StartDate.ToString("yyyy-MM-dd"), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(FetchOutcome.Success);
        await Assert.That(result.ShoppingList).IsNotNull();

        return result.ShoppingList!;
    }

    // Returns the single shopping item in the response.
    private static ShoppingItem SingleItem(Response response) =>
        response.Categories.SelectMany(category => category.Items).Single();

    // Verifies that shopping lists are not available for plans already marked as eaten.
    [Test]
    public async Task ReturnsNotFoundForMarkedAsEatenPlan()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);

        var plan = CreateEmptyWeeklyPlan(StartDate);

        context.WeeklyPlans.Add(plan);
        await context.SaveChangesAsync(CancellationToken.None);

        context.DishConsumptionEvents.Add(new DishConsumptionEvent(
            Guid.NewGuid(),
            StartDate,
            DishHistorySource.WeeklyPlan,
            plan.Id));
        await context.SaveChangesAsync(CancellationToken.None);

        var useCase = new UseCase(context);
        var result = await useCase.Execute("2026-02-02", CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(FetchOutcome.NotFound);
        await Assert.That(result.ShoppingList).IsNull();
    }

    // Verifies that per-serving quantities are multiplied by the household size.
    [Test]
    public async Task ScalesPerServingQuantitiesByHouseholdSize()
    {
        await using var context = CreateContext(Guid.NewGuid().ToString("N"));

        var ingredient = new IngredientEntity("Kjøttdeig", IngredientCategory.Meat, Unit.G);
        var dish = CreateDish("Taco", ingredient.Id, 75, IngredientScaling.PerServing);
        var plan = CreateWeeklyPlan(StartDate, (dish.Id, null));

        var response = await RunAsync(context, plan, [dish], ingredient, householdSize: 3);

        await Assert.That(SingleItem(response).Amount).IsEqualTo(225d);
    }

    // Verifies that a per-day servings override wins over the household default.
    [Test]
    public async Task UsesPlannedDayServingsOverrideWhenPresent()
    {
        await using var context = CreateContext(Guid.NewGuid().ToString("N"));

        var ingredient = new IngredientEntity("Kjøttdeig", IngredientCategory.Meat, Unit.G);
        var dish = CreateDish("Taco", ingredient.Id, 75, IngredientScaling.PerServing);
        var plan = CreateWeeklyPlan(StartDate, (dish.Id, 10));

        var response = await RunAsync(context, plan, [dish], ingredient, householdSize: 3);

        await Assert.That(SingleItem(response).Amount).IsEqualTo(750d);
    }

    // Verifies that per-dish quantities ignore the servings entirely.
    [Test]
    public async Task LeavesPerDishQuantitiesUnscaled()
    {
        await using var context = CreateContext(Guid.NewGuid().ToString("N"));

        var ingredient = new IngredientEntity("Hakkede tomater", IngredientCategory.CannedGoods, Unit.Pcs);
        var dish = CreateDish("Bolognese", ingredient.Id, 1, IngredientScaling.PerDish, unit: Unit.Pcs);
        var plan = CreateWeeklyPlan(StartDate, (dish.Id, 10));

        var response = await RunAsync(context, plan, [dish], ingredient, householdSize: 3);

        await Assert.That(SingleItem(response).Amount).IsEqualTo(1d);
    }

    // Verifies that per-person quantities follow personCount and not the servings.
    [Test]
    public async Task ScalesPerPersonQuantitiesByPersonCountOnly()
    {
        await using var context = CreateContext(Guid.NewGuid().ToString("N"));

        var ingredient = new IngredientEntity("Kyllingfilet", IngredientCategory.Poultry, Unit.G);
        var dish = CreateDish("Salat", ingredient.Id, 150, IngredientScaling.PerPerson, personCount: 1);
        var plan = CreateWeeklyPlan(StartDate, (dish.Id, 10));

        var response = await RunAsync(context, plan, [dish], ingredient, householdSize: 3);

        await Assert.That(SingleItem(response).Amount).IsEqualTo(150d);
    }

    // Verifies that a dish planned on two days contributes its ingredients twice.
    [Test]
    public async Task CountsRepeatedDishOncePerPlannedDay()
    {
        await using var context = CreateContext(Guid.NewGuid().ToString("N"));

        var ingredient = new IngredientEntity("Kjøttdeig", IngredientCategory.Meat, Unit.G);
        var dish = CreateDish("Taco", ingredient.Id, 75, IngredientScaling.PerServing);
        var plan = CreateWeeklyPlan(StartDate, (dish.Id, null), (dish.Id, null));

        var response = await RunAsync(context, plan, [dish], ingredient, householdSize: 3);

        await Assert.That(SingleItem(response).Amount).IsEqualTo(450d);
    }

    // Verifies that pack-denominated amounts accumulate before any rounding to whole packs.
    [Test]
    public async Task AccumulatesPackFractionsAcrossDays()
    {
        await using var context = CreateContext(Guid.NewGuid().ToString("N"));

        var ingredient = new IngredientEntity("Revet ost", IngredientCategory.DairyAndEggs, Unit.Pack);
        var dish = CreateDish("Taco", ingredient.Id, 0.25, IngredientScaling.PerServing, unit: Unit.Pack);
        var plan = CreateWeeklyPlan(StartDate, (dish.Id, null), (dish.Id, null));

        var response = await RunAsync(context, plan, [dish], ingredient, householdSize: 3);

        var item = SingleItem(response);
        await Assert.That(item.Amount).IsEqualTo(1.5d);
        await Assert.That(item.Unit).IsEqualTo(nameof(Unit.Pack));
    }

    // Verifies that the pantry staple flag reaches the shopping list response.
    [Test]
    public async Task SurfacesPantryStapleFlag()
    {
        await using var context = CreateContext(Guid.NewGuid().ToString("N"));

        var ingredient = new IngredientEntity("Ketchup", IngredientCategory.Condiments, Unit.Pcs, true);
        var dish = CreateDish("Pølser", ingredient.Id, 1, IngredientScaling.PerDish, unit: Unit.Pcs);
        var plan = CreateWeeklyPlan(StartDate, (dish.Id, null));

        var response = await RunAsync(context, plan, [dish], ingredient, householdSize: 3);

        await Assert.That(SingleItem(response).IsPantryStaple).IsTrue();
    }
}
