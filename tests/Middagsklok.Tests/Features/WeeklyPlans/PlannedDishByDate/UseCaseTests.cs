using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.Dish;
using Middagsklok.Api.Domain.Ingredient;
using Middagsklok.Api.Domain.WeeklyPlan;
using Middagsklok.Api.Features.WeeklyPlans.PlannedDishByDate;
using TUnit.Assertions;
using TUnit.Core;

namespace Middagsklok.Tests.Features.WeeklyPlans.PlannedDishByDate;

public sealed class UseCaseTests
{
    // Creates an in-memory AppDbContext for test isolation.
    private static AppDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var context = new AppDbContext(options);
        return context;
    }

    // Creates a test ingredient.
    private static Ingredient CreateIngredient(string name, IngredientCategory category = IngredientCategory.Other) =>
        new(name, category, Unit.G);

    // Creates a test dish with ingredients.
    private static Dish CreateDish(
        string name,
        Guid? ingredientId = null,
        string? instructions = null) =>
        new(
            name,
            DishType.Pasta,
            10,
            20,
            4,
            instructions,
            false,
            false,
            false,
            ingredientId.HasValue
                ? new[] { new DishIngredient(ingredientId.Value, 200, Unit.G, "chopped", 1) }
                : Array.Empty<DishIngredient>(),
            new[] { "comfort", "easy" });

    // Creates a test weekly plan with a planned dish for a specific date.
    private static WeeklyPlan CreateWeeklyPlan(DateOnly startDate, Guid dishId, DateOnly plannedDate)
    {
        var days = new[]
        {
            new PlannedDay(plannedDate, new DishSelection(DishSelectionType.Dish, dishId))
        };

        var plan = new WeeklyPlan(startDate, days);
        return plan;
    }

    // Verifies that a valid date with a planned dish returns the dish details with ingredients.
    [Test]
    public async Task ReturnsPlannedDishDetailsWithIngredients()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);

        var ingredient = CreateIngredient("Tomato");
        var dish = CreateDish("Pasta Arrabbiata", ingredient.Id, "Cook and enjoy");
        var targetDate = new DateOnly(2026, 2, 27);
        var weeklyPlan = CreateWeeklyPlan(new DateOnly(2026, 2, 24), dish.Id, targetDate);

        context.Ingredients.Add(ingredient);
        context.Dishes.Add(dish);
        context.WeeklyPlans.Add(weeklyPlan);
        await context.SaveChangesAsync(CancellationToken.None);

        var useCase = new UseCase(context);
        var result = await useCase.Execute("2026-02-27", CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(FetchOutcome.Success);
        await Assert.That(result.Data).IsNotNull();

        var response = result.Data!;
        await Assert.That(response.Date).IsEqualTo("2026-02-27");
        await Assert.That(response.Dish.Name).IsEqualTo("Pasta Arrabbiata");
        await Assert.That(response.Dish.Instructions).IsEqualTo("Cook and enjoy");
        await Assert.That(response.Dish.Ingredients).Count().IsEqualTo(1);

        var returnedIngredient = response.Dish.Ingredients.First();
        await Assert.That(returnedIngredient.Name).IsEqualTo("Tomato");
        await Assert.That(returnedIngredient.Quantity).IsEqualTo(200);
        await Assert.That(returnedIngredient.Unit).IsEqualTo("g");
        await Assert.That(returnedIngredient.Note).IsEqualTo("chopped");
    }

    // Verifies that a date with an empty selection returns not found.
    [Test]
    public async Task ReturnsNotFoundWhenDateHasEmptySelection()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);

        var targetDate = new DateOnly(2026, 2, 27);
        var days = new[]
        {
            new PlannedDay(targetDate, new DishSelection(DishSelectionType.Empty, null))
        };
        var weeklyPlan = new WeeklyPlan(new DateOnly(2026, 2, 24), days);

        context.WeeklyPlans.Add(weeklyPlan);
        await context.SaveChangesAsync(CancellationToken.None);

        var useCase = new UseCase(context);
        var result = await useCase.Execute("2026-02-27", CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(FetchOutcome.NotFound);
        await Assert.That(result.ErrorMessage).IsEqualTo("No dish planned for the specified date.");
    }

    // Verifies that a date without a weekly plan returns not found.
    [Test]
    public async Task ReturnsNotFoundWhenNoWeeklyPlanExists()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);

        var useCase = new UseCase(context);
        var result = await useCase.Execute("2026-02-27", CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(FetchOutcome.NotFound);
        await Assert.That(result.ErrorMessage).IsEqualTo("No weekly plan found for the specified date.");
    }

    // Verifies that an invalid date format returns invalid outcome.
    [Test]
    public async Task ReturnsInvalidForBadDateFormat()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);

        var useCase = new UseCase(context);
        var result = await useCase.Execute("27-02-2026", CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(FetchOutcome.Invalid);
        await Assert.That(result.ErrorMessage).Contains("ISO-8601");
    }

    // Verifies that a null date parameter returns invalid outcome.
    [Test]
    public async Task ReturnsInvalidForNullDate()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);

        var useCase = new UseCase(context);
        var result = await useCase.Execute(null, CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(FetchOutcome.Invalid);
        await Assert.That(result.ErrorMessage).IsEqualTo("Date is required.");
    }

    // Verifies that dishes with vibe tags return those tags in the response.
    [Test]
    public async Task ReturnsDishWithVibeTags()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);

        var dish = CreateDish("Comfort Pasta");
        var targetDate = new DateOnly(2026, 2, 27);
        var weeklyPlan = CreateWeeklyPlan(new DateOnly(2026, 2, 24), dish.Id, targetDate);

        context.Dishes.Add(dish);
        context.WeeklyPlans.Add(weeklyPlan);
        await context.SaveChangesAsync(CancellationToken.None);

        var useCase = new UseCase(context);
        var result = await useCase.Execute("2026-02-27", CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(FetchOutcome.Success);
        await Assert.That(result.Data).IsNotNull();

        var response = result.Data!;
        await Assert.That(response.Dish.VibeTags).Count().IsEqualTo(2);
        await Assert.That(response.Dish.VibeTags).Contains("comfort");
        await Assert.That(response.Dish.VibeTags).Contains("easy");
    }
}
