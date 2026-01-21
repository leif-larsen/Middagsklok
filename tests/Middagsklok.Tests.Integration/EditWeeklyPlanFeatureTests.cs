using Microsoft.EntityFrameworkCore;
using Middagsklok.Database;
using Middagsklok.Database.Repositories;
using Middagsklok.Domain;
using Middagsklok.Features.WeeklyPlans.Edit;
using Middagsklok.Features.WeeklyPlans.Generate;

namespace Middagsklok.Tests.Integration;

public class EditWeeklyPlanFeatureTests : IDisposable
{
    private readonly MiddagsklokDbContext _context;
    private readonly EditWeeklyPlanFeature _feature;
    private readonly DishRepository _dishRepository;
    private readonly DateOnly _weekStart = new(2026, 1, 5); // Monday

    public EditWeeklyPlanFeatureTests()
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
        var validator = new WeeklyPlanRulesValidator();
        var rules = new PlanningRules();
        _feature = new EditWeeklyPlanFeature(_dishRepository, weeklyPlanRepository, validator, rules);
    }

    [Fact]
    public async Task Execute_WithDuplicateDishes_ReturnsViolation()
    {
        // Arrange
        var dishes = await _dishRepository.GetAllWithIngredients();
        var firstDish = dishes[0];
        var items = Enumerable.Range(0, 7).Select(i => 
            new EditWeeklyPlanItemRequest(i, firstDish.Id)).ToList(); // Same dish for all days
        var request = new EditWeeklyPlanRequest(_weekStart, items);

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _feature.Execute(request));

        // Assert
        Assert.Contains("duplicate dishes", exception.Message);
    }

    [Fact]
    public async Task Execute_WithInvalidDishId_ThrowsArgumentException()
    {
        // Arrange
        var items = Enumerable.Range(0, 7).Select(i => 
            new EditWeeklyPlanItemRequest(i, Guid.NewGuid())).ToList();
        var request = new EditWeeklyPlanRequest(_weekStart, items);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _feature.Execute(request));
    }

    [Fact]
    public async Task Execute_WithIncorrectDayCount_ThrowsArgumentException()
    {
        // Arrange
        var dishes = await _dishRepository.GetAllWithIngredients();
        var items = Enumerable.Range(0, 5).Select(i => // Only 5 days
            new EditWeeklyPlanItemRequest(i, dishes[0].Id)).ToList();
        var request = new EditWeeklyPlanRequest(_weekStart, items);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _feature.Execute(request));
        Assert.Contains("exactly 7 items", exception.Message);
    }

    [Fact]
    public async Task Execute_WithDuplicateDayIndex_ThrowsArgumentException()
    {
        // Arrange
        var dishes = await _dishRepository.GetAllWithIngredients();
        var items = new List<EditWeeklyPlanItemRequest>
        {
            new(0, dishes[0].Id),
            new(1, dishes[0].Id),
            new(2, dishes[0].Id),
            new(3, dishes[0].Id),
            new(4, dishes[0].Id),
            new(5, dishes[0].Id),
            new(5, dishes[0].Id) // Duplicate day 5
        };
        var request = new EditWeeklyPlanRequest(_weekStart, items);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _feature.Execute(request));
        Assert.Contains("missing days", exception.Message); // Will report missing day 6
    }

    [Fact]
    public async Task Execute_WithMissingDayIndex_ThrowsArgumentException()
    {
        // Arrange
        var dishes = await _dishRepository.GetAllWithIngredients();
        var items = new List<EditWeeklyPlanItemRequest>
        {
            new(0, dishes[0].Id),
            new(1, dishes[0].Id),
            new(2, dishes[0].Id),
            new(3, dishes[0].Id),
            new(4, dishes[0].Id),
            new(5, dishes[0].Id),
            new(7, dishes[0].Id) // Day 7 is invalid (should be 0-6)
        };
        var request = new EditWeeklyPlanRequest(_weekStart, items);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _feature.Execute(request));
        Assert.Contains("missing days", exception.Message); // Will report missing day 6
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
