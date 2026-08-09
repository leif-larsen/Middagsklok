using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.Dish;
using Middagsklok.Api.Domain.DishHistory;
using Middagsklok.Api.Domain.WeeklyPlan;
using Middagsklok.Api.Features.Dishes.Delete;
using TUnit.Assertions;
using TUnit.Core;

namespace Middagsklok.Tests.Features.Dishes.Delete;

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

    // Creates a minimal dish for testing.
    private static Dish CreateDish(string name = "Test Dish") =>
        new(name, DishType.Other, 10, 20, 4, null, false, false, false, Array.Empty<DishIngredient>());

    // Verifies that delete succeeds for an unreferenced dish.
    [Test]
    public async Task DeletesUnreferencedDish()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);

        var dish = CreateDish();
        context.Dishes.Add(dish);
        await context.SaveChangesAsync(CancellationToken.None);

        var useCase = new UseCase(context);
        var result = await useCase.Execute(dish.Id.ToString("D"), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(DeleteOutcome.Success);

        var remainingDish = await context.Dishes.FindAsync(dish.Id);
        await Assert.That(remainingDish).IsNull();
    }

    // Verifies that delete returns Conflict when the dish is referenced by a weekly plan.
    [Test]
    public async Task ReturnsConflictWhenDishReferencedByWeeklyPlan()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);

        var dish = CreateDish();
        context.Dishes.Add(dish);

        var plan = new WeeklyPlan(
            new DateOnly(2026, 7, 7),
            [new PlannedDay(new DateOnly(2026, 7, 7), new DishSelection(DishSelectionType.Dish, dish.Id))]);
        context.WeeklyPlans.Add(plan);
        await context.SaveChangesAsync(CancellationToken.None);

        var useCase = new UseCase(context);
        var result = await useCase.Execute(dish.Id.ToString("D"), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(DeleteOutcome.Conflict);
        await Assert.That(result.Errors.Count).IsGreaterThan(0);

        var remainingDish = await context.Dishes.FindAsync(dish.Id);
        await Assert.That(remainingDish).IsNotNull();
    }

    // Verifies that delete returns Conflict when the dish has a consumption event.
    [Test]
    public async Task ReturnsConflictWhenDishHasConsumptionEvent()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);

        var dish = CreateDish();
        context.Dishes.Add(dish);

        var evt = new DishConsumptionEvent(dish.Id, new DateOnly(2026, 7, 7), DishHistorySource.Manual, null);
        context.DishConsumptionEvents.Add(evt);
        await context.SaveChangesAsync(CancellationToken.None);

        var useCase = new UseCase(context);
        var result = await useCase.Execute(dish.Id.ToString("D"), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(DeleteOutcome.Conflict);
        await Assert.That(result.Errors.Count).IsGreaterThan(0);

        var remainingDish = await context.Dishes.FindAsync(dish.Id);
        await Assert.That(remainingDish).IsNotNull();
    }
}
