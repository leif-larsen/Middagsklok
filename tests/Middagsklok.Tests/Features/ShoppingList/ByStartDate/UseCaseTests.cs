using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.DishHistory;
using Middagsklok.Api.Domain.WeeklyPlan;
using Middagsklok.Api.Features.ShoppingList.ByStartDate;
using TUnit.Assertions;
using TUnit.Core;

namespace Middagsklok.Tests.Features.ShoppingList.ByStartDate;

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

    // Verifies that shopping lists are not available for plans already marked as eaten.
    [Test]
    public async Task ReturnsNotFoundForMarkedAsEatenPlan()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);

        var startDate = new DateOnly(2026, 2, 2);
        var plan = CreateEmptyWeeklyPlan(startDate);

        context.WeeklyPlans.Add(plan);
        await context.SaveChangesAsync(CancellationToken.None);

        context.DishConsumptionEvents.Add(new DishConsumptionEvent(
            Guid.NewGuid(),
            startDate,
            DishHistorySource.WeeklyPlan,
            plan.Id));
        await context.SaveChangesAsync(CancellationToken.None);

        var useCase = new UseCase(context);
        var result = await useCase.Execute("2026-02-02", CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(FetchOutcome.NotFound);
        await Assert.That(result.ShoppingList).IsNull();
    }
}
