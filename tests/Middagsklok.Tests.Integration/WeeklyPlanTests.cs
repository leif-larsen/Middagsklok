using Microsoft.EntityFrameworkCore;
using Middagsklok.Database;
using Middagsklok.Database.Repositories;
using Middagsklok.Domain;
using Middagsklok.Features.ShoppingList.GenerateForWeek;
using Middagsklok.Features.WeeklyPlans.Create;

namespace Middagsklok.Tests.Integration;

public class WeeklyPlanRepositoryTests : IDisposable
{
    private readonly MiddagsklokDbContext _context;
    private readonly WeeklyPlanRepository _repository;
    private readonly DishRepository _dishRepository;
    private readonly DateOnly _weekStart = new(2026, 1, 5); // Monday

    public WeeklyPlanRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MiddagsklokDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new MiddagsklokDbContext(options);
        _context.Database.OpenConnection();

        var bootstrapper = new DbBootstrapper(_context);
        bootstrapper.InitializeAsync().GetAwaiter().GetResult();

        _repository = new WeeklyPlanRepository(_context);
        _dishRepository = new DishRepository(_context);
    }

    [Fact]
    public async Task CreateOrReplace_PersistsSevenItems()
    {
        var dishes = await _dishRepository.GetAllWithIngredients();
        var plan = CreateTestPlan(dishes);

        var result = await _repository.CreateOrReplace(plan);

        Assert.Equal(7, result.Items.Count);
        Assert.Equal(_weekStart, result.WeekStartDate);
    }

    [Fact]
    public async Task CreateOrReplace_ReplacesExistingPlanForSameWeek()
    {
        var dishes = await _dishRepository.GetAllWithIngredients();
        
        var plan1 = CreateTestPlan(dishes);
        await _repository.CreateOrReplace(plan1);

        var plan2 = CreateTestPlan(dishes);
        var result = await _repository.CreateOrReplace(plan2);

        // Should only have one plan for this week
        var loadedPlan = await _repository.GetByWeekStartDate(_weekStart);
        Assert.NotNull(loadedPlan);
        Assert.Equal(plan2.Id, result.Id);
    }

    [Fact]
    public async Task GetByWeekStartDate_ReturnsItemsOrderedByDayIndex()
    {
        var dishes = await _dishRepository.GetAllWithIngredients();
        var plan = CreateTestPlan(dishes);
        await _repository.CreateOrReplace(plan);

        var result = await _repository.GetByWeekStartDate(_weekStart);

        Assert.NotNull(result);
        var dayIndices = result.Items.Select(i => i.DayIndex).ToList();
        Assert.Equal([0, 1, 2, 3, 4, 5, 6], dayIndices);
    }

    [Fact]
    public async Task GetByWeekStartDate_ReturnsDishWithIngredients()
    {
        var dishes = await _dishRepository.GetAllWithIngredients();
        var plan = CreateTestPlan(dishes);
        await _repository.CreateOrReplace(plan);

        var result = await _repository.GetByWeekStartDate(_weekStart);

        Assert.NotNull(result);
        var firstItem = result.Items.First();
        Assert.NotNull(firstItem.Dish);
        Assert.NotEmpty(firstItem.Dish.Ingredients);
    }

    [Fact]
    public async Task GetByWeekStartDate_ReturnsNullWhenNotFound()
    {
        var result = await _repository.GetByWeekStartDate(new DateOnly(2099, 1, 1));

        Assert.Null(result);
    }

    private WeeklyPlan CreateTestPlan(IReadOnlyList<Dish> dishes)
    {
        var items = new List<WeeklyPlanItem>();
        for (var i = 0; i < 7; i++)
        {
            items.Add(new WeeklyPlanItem(i, dishes[i % dishes.Count]));
        }

        return new WeeklyPlan(
            Id: Guid.NewGuid(),
            WeekStartDate: _weekStart,
            CreatedAt: DateTimeOffset.UtcNow,
            Items: items);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

public class CreateWeeklyPlanFeatureTests : IDisposable
{
    private readonly MiddagsklokDbContext _context;
    private readonly CreateWeeklyPlanFeature _feature;
    private readonly DishRepository _dishRepository;

    public CreateWeeklyPlanFeatureTests()
    {
        var options = new DbContextOptionsBuilder<MiddagsklokDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new MiddagsklokDbContext(options);
        _context.Database.OpenConnection();

        var bootstrapper = new DbBootstrapper(_context);
        bootstrapper.InitializeAsync().GetAwaiter().GetResult();

        _dishRepository = new DishRepository(_context);
        var weeklyPlanRepository = new WeeklyPlanRepository(_context);
        _feature = new CreateWeeklyPlanFeature(_dishRepository, weeklyPlanRepository);
    }

    [Fact]
    public async Task Execute_CreatesPlanWithSevenDishes()
    {
        var dishes = await _dishRepository.GetAllWithIngredients();
        var dishIds = Enumerable.Range(0, 7).Select(i => dishes[i % dishes.Count].Id).ToList();
        var request = new CreateWeeklyPlanRequest(new DateOnly(2026, 1, 5), dishIds);

        var result = await _feature.Execute(request);

        Assert.Equal(7, result.Items.Count);
    }

    [Fact]
    public async Task Execute_ThrowsWhenNotSevenDishes()
    {
        var request = new CreateWeeklyPlanRequest(new DateOnly(2026, 1, 5), [Guid.NewGuid()]);

        await Assert.ThrowsAsync<ArgumentException>(() => _feature.Execute(request));
    }

    [Fact]
    public async Task Execute_ThrowsWhenDishNotFound()
    {
        var dishIds = Enumerable.Range(0, 7).Select(_ => Guid.NewGuid()).ToList();
        var request = new CreateWeeklyPlanRequest(new DateOnly(2026, 1, 5), dishIds);

        await Assert.ThrowsAsync<ArgumentException>(() => _feature.Execute(request));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

public class CreateShoppingListForWeekFeatureTests : IDisposable
{
    private readonly MiddagsklokDbContext _context;
    private readonly CreateShoppingListForWeekFeature _shoppingListFeature;
    private readonly CreateWeeklyPlanFeature _createPlanFeature;
    private readonly DishRepository _dishRepository;
    private readonly DateOnly _weekStart = new(2026, 1, 5);

    public CreateShoppingListForWeekFeatureTests()
    {
        var options = new DbContextOptionsBuilder<MiddagsklokDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new MiddagsklokDbContext(options);
        _context.Database.OpenConnection();

        var bootstrapper = new DbBootstrapper(_context);
        bootstrapper.InitializeAsync().GetAwaiter().GetResult();

        _dishRepository = new DishRepository(_context);
        var weeklyPlanRepository = new WeeklyPlanRepository(_context);
        _createPlanFeature = new CreateWeeklyPlanFeature(_dishRepository, weeklyPlanRepository);
        _shoppingListFeature = new CreateShoppingListForWeekFeature(weeklyPlanRepository);
    }

    [Fact]
    public async Task Execute_AggregatesSharedIngredients()
    {
        // Create a plan with dishes that share ingredients
        var dishes = await _dishRepository.GetAllWithIngredients();
        var dishIds = Enumerable.Range(0, 7).Select(i => dishes[i % dishes.Count].Id).ToList();
        var request = new CreateWeeklyPlanRequest(_weekStart, dishIds);
        await _createPlanFeature.Execute(request);

        var result = await _shoppingListFeature.Execute(_weekStart);

        Assert.NotNull(result);
        // With 2 dishes and 7 days, ingredients should be aggregated
        var pastaItem = result.Items.FirstOrDefault(i => i.IngredientName == "Pasta");
        Assert.NotNull(pastaItem);
        // Both dishes have 400g pasta, cycling 7 days: 4*400 + 3*400 = 2800g
        Assert.Equal(2800m, pastaItem.Amount);
    }

    [Fact]
    public async Task Execute_IgnoresOptionalIngredients()
    {
        var dishes = await _dishRepository.GetAllWithIngredients();
        var dishIds = Enumerable.Range(0, 7).Select(i => dishes[i % dishes.Count].Id).ToList();
        var request = new CreateWeeklyPlanRequest(_weekStart, dishIds);
        await _createPlanFeature.Execute(request);

        var result = await _shoppingListFeature.Execute(_weekStart);

        Assert.NotNull(result);
        // Parmesan and Hvitløk are optional in some recipes
        var parmesanItem = result.Items.FirstOrDefault(i => i.IngredientName == "Parmesan");
        // Parmesan is optional in pasta med tomatsaus, should not appear
        Assert.Null(parmesanItem);
    }

    [Fact]
    public async Task Execute_OrdersByCategoryThenName()
    {
        var dishes = await _dishRepository.GetAllWithIngredients();
        var dishIds = Enumerable.Range(0, 7).Select(i => dishes[i % dishes.Count].Id).ToList();
        var request = new CreateWeeklyPlanRequest(_weekStart, dishIds);
        await _createPlanFeature.Execute(request);

        var result = await _shoppingListFeature.Execute(_weekStart);

        Assert.NotNull(result);
        var items = result.Items.ToList();
        var sortedItems = items.OrderBy(i => i.Category).ThenBy(i => i.IngredientName).ToList();
        
        for (var i = 0; i < items.Count; i++)
        {
            Assert.Equal(sortedItems[i].IngredientName, items[i].IngredientName);
        }
    }

    [Fact]
    public async Task Execute_ReturnsNullWhenNoPlan()
    {
        var result = await _shoppingListFeature.Execute(new DateOnly(2099, 1, 1));

        Assert.Null(result);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
