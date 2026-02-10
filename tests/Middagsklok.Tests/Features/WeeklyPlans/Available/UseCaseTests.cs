using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.DishHistory;
using Middagsklok.Api.Domain.WeeklyPlan;
using Middagsklok.Api.Features.WeeklyPlans.Available;
using TUnit.Assertions;
using TUnit.Core;

namespace Middagsklok.Tests.Features.WeeklyPlans.Available;

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

    // Verifies that eaten plans are excluded from available weekly plans.
    [Test]
    public async Task ExcludesMarkedAsEatenPlans()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);

        var eatenPlan = CreateEmptyWeeklyPlan(new DateOnly(2026, 2, 2));
        var upcomingPlan = CreateEmptyWeeklyPlan(new DateOnly(2026, 2, 9));

        context.WeeklyPlans.AddRange(eatenPlan, upcomingPlan);
        await context.SaveChangesAsync(CancellationToken.None);

        context.DishConsumptionEvents.Add(new DishConsumptionEvent(
            Guid.NewGuid(),
            new DateOnly(2026, 2, 3),
            DishHistorySource.WeeklyPlan,
            eatenPlan.Id));
        await context.SaveChangesAsync(CancellationToken.None);

        var useCase = new UseCase(context);
        var response = await useCase.Execute(CancellationToken.None);

        await Assert.That(response.Plans.Count()).IsEqualTo(1);
        await Assert.That(response.Plans.Single().StartDate).IsEqualTo("2026-02-09");
    }
}
