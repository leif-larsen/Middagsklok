using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.Dish;
using Middagsklok.Api.Domain.DishHistory;
using Middagsklok.Api.Domain.WeeklyPlan;
using Middagsklok.Api.Features.WeeklyPlans.Upsert;
using TUnit.Assertions;
using TUnit.Core;

namespace Middagsklok.Tests.Features.WeeklyPlans.Upsert;

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

    // Creates a minimal valid dish for test data setup.
    private static Dish CreateDish(string name) =>
        new(
            name,
            DishType.Other,
            10,
            20,
            4,
            null,
            false,
            false,
            false,
            Array.Empty<DishIngredient>());

    // Creates a weekly plan with empty selections for all seven days.
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

    // Creates a valid upsert request that covers all seven days.
    private static Request CreateEmptyRequest(DateOnly startDate)
    {
        var days = Enumerable.Range(0, 7)
            .Select(offset => new PlannedDayInput(
                startDate.AddDays(offset).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                new DishSelectionInput("EMPTY", null)))
            .ToArray();

        var request = new Request(days);
        return request;
    }

    // Verifies that upsert is blocked when the weekly plan is already marked as eaten.
    [Test]
    public async Task ReturnsConflictWhenExistingPlanIsMarkedAsEaten()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);

        const string startDateValue = "2026-02-02";
        var startDate = DateOnly.ParseExact(startDateValue, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var dish = CreateDish("Tomato Soup");
        var plan = CreateEmptyWeeklyPlan(startDate);
        var request = CreateEmptyRequest(startDate);

        context.Dishes.Add(dish);
        context.WeeklyPlans.Add(plan);
        await context.SaveChangesAsync(CancellationToken.None);

        context.DishConsumptionEvents.Add(new DishConsumptionEvent(
            dish.Id,
            startDate,
            DishHistorySource.WeeklyPlan,
            plan.Id));
        await context.SaveChangesAsync(CancellationToken.None);

        var useCase = new UseCase(context);
        var result = await useCase.Execute(startDateValue, request, CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(UpsertOutcome.Conflict);
        await Assert.That(result.Plan).IsNull();
    }
}
