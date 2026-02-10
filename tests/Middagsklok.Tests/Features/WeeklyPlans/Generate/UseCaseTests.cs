using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.Dish;
using Middagsklok.Api.Domain.DishHistory;
using Middagsklok.Api.Domain.Settings;
using Middagsklok.Api.Domain.WeeklyPlan;
using Middagsklok.Api.Features.WeeklyPlans.Generate;
using TUnit.Assertions;
using TUnit.Core;

namespace Middagsklok.Tests.Features.WeeklyPlans.Generate;

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

    // Creates a dish with a seafood classification.
    private static Dish CreateDish(string name, bool isSeafood, CuisineType cuisine = CuisineType.Other) =>
        new(
            name,
            cuisine,
            10,
            20,
            4,
            null,
            isSeafood,
            false,
            false,
            Array.Empty<DishIngredient>());

    // Counts seafood selections in a generated plan.
    private static int CountSeafoodDays(Response plan, IReadOnlyDictionary<string, bool> seafoodByDishId) =>
        plan.Days.Count(day =>
            day.Selection.DishId is not null
            && seafoodByDishId.TryGetValue(day.Selection.DishId, out var isSeafood)
            && isSeafood);

    // Counts unique dish selections in a generated plan.
    private static int CountUniqueDishes(Response plan) =>
        plan.Days
            .Select(day => day.Selection.DishId)
            .Where(dishId => dishId is not null)
            .Distinct(StringComparer.Ordinal)
            .Count();

    // Verifies that generation picks the exact configured seafood count when both pools exist.
    [Test]
    public async Task GeneratesExactSeafoodCountWhenPoolsAreAvailable()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);

        context.PlanningSettings.Add(new PlanningSettings(DayOfWeek.Monday, 2));
        context.Dishes.AddRange(
            CreateDish("Baked Salmon", true),
            CreateDish("Shrimp Rice", true),
            CreateDish("Pasta Arrabbiata", false),
            CreateDish("Tomato Soup", false),
            CreateDish("Chicken Curry", false),
            CreateDish("Veggie Tacos", false),
            CreateDish("Mushroom Risotto", false));
        await context.SaveChangesAsync(CancellationToken.None);

        var useCase = new UseCase(context);
        var result = await useCase.Execute("2026-02-02", CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(GenerateOutcome.Success);
        await Assert.That(result.Plan).IsNotNull();

        var plan = result.Plan!;
        var seafoodByDishId = context.Dishes
            .AsNoTracking()
            .ToDictionary(dish => dish.Id.ToString("D"), dish => dish.IsSeafood);
        var seafoodCount = CountSeafoodDays(plan, seafoodByDishId);
        var uniqueDishCount = CountUniqueDishes(plan);

        await Assert.That(seafoodCount).IsEqualTo(2);
        await Assert.That(uniqueDishCount).IsEqualTo(7);
        await Assert.That(plan.Notes.Count).IsEqualTo(0);
    }

    // Verifies that generation relaxes the seafood rule and reports notes when seafood dishes are unavailable.
    [Test]
    public async Task RelaxesSeafoodRuleWhenSeafoodDishesAreUnavailable()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);

        context.PlanningSettings.Add(new PlanningSettings(DayOfWeek.Monday, 2));
        context.Dishes.Add(CreateDish("Tomato Soup", false));
        await context.SaveChangesAsync(CancellationToken.None);

        var useCase = new UseCase(context);
        var result = await useCase.Execute("2026-02-02", CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(GenerateOutcome.Success);
        await Assert.That(result.Plan).IsNotNull();

        var plan = result.Plan!;
        var seafoodByDishId = context.Dishes
            .AsNoTracking()
            .ToDictionary(dish => dish.Id.ToString("D"), dish => dish.IsSeafood);
        var seafoodCount = CountSeafoodDays(plan, seafoodByDishId);
        var hasRelaxedNote = plan.Notes.Any(note =>
            note.Contains("Seafood target was relaxed because no seafood dishes are available.", StringComparison.Ordinal));
        var hasCountMismatchNote = plan.Notes.Any(note =>
            note.Contains("Requested 2 seafood dish(es), generated 0.", StringComparison.Ordinal));

        await Assert.That(seafoodCount).IsEqualTo(0);
        await Assert.That(hasRelaxedNote).IsTrue();
        await Assert.That(hasCountMismatchNote).IsTrue();
    }

    // Verifies that generation relaxes uniqueness and reports notes when too few unique dishes are available.
    [Test]
    public async Task RelaxesUniquenessWhenTooFewDishesAreAvailable()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);

        context.PlanningSettings.Add(new PlanningSettings(DayOfWeek.Monday, 0));
        context.Dishes.AddRange(
            CreateDish("Pasta Arrabbiata", false),
            CreateDish("Tomato Soup", false));
        await context.SaveChangesAsync(CancellationToken.None);

        var useCase = new UseCase(context);
        var result = await useCase.Execute("2026-02-02", CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(GenerateOutcome.Success);
        await Assert.That(result.Plan).IsNotNull();

        var plan = result.Plan!;
        var uniqueDishCount = CountUniqueDishes(plan);
        var hasUniquenessNote = plan.Notes.Any(note =>
            note.Contains("Dish uniqueness was relaxed because only 2 unique dish(es) are available for 7 day(s).", StringComparison.Ordinal));

        await Assert.That(uniqueDishCount).IsEqualTo(2);
        await Assert.That(hasUniquenessNote).IsTrue();
    }

    // Verifies that generation is blocked when the weekly plan is already marked as eaten.
    [Test]
    public async Task ReturnsConflictWhenExistingPlanIsMarkedAsEaten()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);

        var startDate = new DateOnly(2026, 2, 2);
        var dish = CreateDish("Baked Salmon", true);
        var days = Enumerable.Range(0, 7)
            .Select(offset => new PlannedDay(
                startDate.AddDays(offset),
                new DishSelection(DishSelectionType.Empty, null)))
            .ToArray();
        var plan = new WeeklyPlan(startDate, days);

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
        var result = await useCase.Execute("2026-02-02", CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(GenerateOutcome.Conflict);
        await Assert.That(result.Plan).IsNull();
    }

    // Verifies that type weights can change the first-day pick between weekday and weekend.
    [Test]
    public async Task AppliesDifferentTypeWeightsForWeekdayAndWeekend()
    {
        var weekdayDatabaseName = Guid.NewGuid().ToString("N");
        await using var weekdayContext = CreateContext(weekdayDatabaseName);

        weekdayContext.PlanningSettings.Add(new PlanningSettings(DayOfWeek.Monday, 0));
        weekdayContext.Dishes.AddRange(
            CreateDish("Alpha Pizza", false, CuisineType.PizzaPie),
            CreateDish("Beta Salad", false, CuisineType.Salad));
        await weekdayContext.SaveChangesAsync(CancellationToken.None);

        var deterministicRandom = new DeterministicRandomSource(0.55);
        var weekdayUseCase = new UseCase(weekdayContext, deterministicRandom);
        var weekdayResult = await weekdayUseCase.Execute("2026-02-02", CancellationToken.None);

        await Assert.That(weekdayResult.Outcome).IsEqualTo(GenerateOutcome.Success);
        await Assert.That(weekdayResult.Plan).IsNotNull();

        var weekdayDishNamesById = weekdayContext.Dishes
            .AsNoTracking()
            .ToDictionary(dish => dish.Id.ToString("D"), dish => dish.Name);
        var weekdayFirstDishId = weekdayResult.Plan!.Days.First().Selection.DishId;
        await Assert.That(weekdayFirstDishId).IsNotNull();

        var weekdayFirstDishName = weekdayDishNamesById[weekdayFirstDishId!];
        await Assert.That(weekdayFirstDishName).IsEqualTo("Beta Salad");

        var weekendDatabaseName = Guid.NewGuid().ToString("N");
        await using var weekendContext = CreateContext(weekendDatabaseName);

        weekendContext.PlanningSettings.Add(new PlanningSettings(DayOfWeek.Monday, 0));
        weekendContext.Dishes.AddRange(
            CreateDish("Alpha Pizza", false, CuisineType.PizzaPie),
            CreateDish("Beta Salad", false, CuisineType.Salad));
        await weekendContext.SaveChangesAsync(CancellationToken.None);

        var weekendUseCase = new UseCase(weekendContext, deterministicRandom);
        var weekendResult = await weekendUseCase.Execute("2026-02-07", CancellationToken.None);

        await Assert.That(weekendResult.Outcome).IsEqualTo(GenerateOutcome.Success);
        await Assert.That(weekendResult.Plan).IsNotNull();

        var weekendDishNamesById = weekendContext.Dishes
            .AsNoTracking()
            .ToDictionary(dish => dish.Id.ToString("D"), dish => dish.Name);
        var weekendFirstDishId = weekendResult.Plan!.Days.First().Selection.DishId;
        await Assert.That(weekendFirstDishId).IsNotNull();

        var weekendFirstDishName = weekendDishNamesById[weekendFirstDishId!];
        await Assert.That(weekendFirstDishName).IsEqualTo("Alpha Pizza");
    }
}

internal sealed class DeterministicRandomSource(double nextDoubleValue) : IRandomSource
{
    private readonly double _nextDoubleValue = nextDoubleValue;

    // Returns zero so shuffle becomes deterministic.
    public int Next(int maxValue) => 0;

    // Returns a fixed random value for deterministic weighted picks.
    public double NextDouble() => _nextDoubleValue;
}
