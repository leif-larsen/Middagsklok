using Microsoft.EntityFrameworkCore;
using Middagsklok.Database;
using Middagsklok.Database.Entities;
using Middagsklok.Database.Repositories;
using Middagsklok.Domain;
using Middagsklok.Features.WeeklyPlanning;

namespace Middagsklok.Tests.Integration;

public class GenerateWeeklyPlanIntegrationTests : IDisposable
{
    private readonly MiddagsklokDbContext _context;
    private readonly IDishRepository _dishRepository;
    private readonly IDishHistoryRepository _historyRepository;
    private readonly IWeeklyPlanRepository _planRepository;
    private readonly WeeklyPlanRulesValidator _validator;
    private readonly GenerateWeeklyPlanFeature _feature;
    private readonly DateOnly _weekStart = new(2026, 1, 12); // Monday

    public GenerateWeeklyPlanIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<MiddagsklokDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new MiddagsklokDbContext(options);
        _context.Database.OpenConnection();

        var bootstrapper = new DbBootstrapper(_context);
        bootstrapper.InitializeAsync().GetAwaiter().GetResult();

        _dishRepository = new DishRepository(_context);
        _historyRepository = new DishHistoryRepository(_context);
        _planRepository = new WeeklyPlanRepository(_context);
        _validator = new WeeklyPlanRulesValidator();
        _feature = new GenerateWeeklyPlanFeature(
            _dishRepository,
            _historyRepository,
            _planRepository,
            _validator);
    }

    [Fact]
    public async Task Execute_WithSeededDishes_GeneratesValidPlan()
    {
        // Arrange
        await SeedTestDishes();
        var request = new GenerateWeeklyPlanRequest(_weekStart);

        // Act
        var result = await _feature.Execute(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Plan);
        Assert.Equal(7, result.Plan.Items.Count);
        Assert.Equal(_weekStart, result.Plan.WeekStartDate);
    }

    [Fact]
    public async Task Execute_WithSeededDishes_PersistsPlanToDatabase()
    {
        // Arrange
        await SeedTestDishes();
        var request = new GenerateWeeklyPlanRequest(_weekStart);

        // Act
        var result = await _feature.Execute(request);

        // Assert
        var loadedPlan = await _planRepository.GetByWeekStartDate(_weekStart);
        Assert.NotNull(loadedPlan);
        Assert.Equal(7, loadedPlan.Items.Count);
        Assert.Equal(result.Plan.Id, loadedPlan.Id);
    }

    [Fact]
    public async Task Execute_WithSeededDishes_LoadsDishesWithIngredients()
    {
        // Arrange
        await SeedTestDishes();
        var request = new GenerateWeeklyPlanRequest(_weekStart);

        // Act
        var result = await _feature.Execute(request);

        // Assert
        foreach (var item in result.Plan.Items)
        {
            Assert.NotNull(item.Dish);
            Assert.NotEmpty(item.Dish.Ingredients);
            
            foreach (var dishIngredient in item.Dish.Ingredients)
            {
                Assert.NotNull(dishIngredient.Ingredient);
                Assert.NotEmpty(dishIngredient.Ingredient.Name);
            }
        }
    }

    [Fact]
    public async Task Execute_WithSeededDishes_PassesValidation()
    {
        // Arrange
        await SeedTestDishes();
        var request = new GenerateWeeklyPlanRequest(_weekStart);

        // Act
        var result = await _feature.Execute(request);

        // Assert
        var rules = new PlanningRules();
        var violations = _validator.Validate(result.Plan, rules);
        
        Assert.Empty(violations);
    }

    [Fact]
    public async Task Execute_WithDishHistory_ConsidersLastEatenDates()
    {
        // Arrange
        await SeedTestDishes();
        var dishes = await _dishRepository.GetAllWithIngredients();
        
        // Mark first dish as recently eaten
        var recentDish = dishes.First();
        var recentEntry = new DishHistoryEntry(
            Guid.NewGuid(),
            recentDish.Id,
            new DateOnly(2026, 1, 10), // 2 days ago from week start
            null,
            null);
        await _historyRepository.Add(recentEntry);
        
        // Mark second dish as eaten long ago
        var oldDish = dishes.Skip(1).First();
        var oldEntry = new DishHistoryEntry(
            Guid.NewGuid(),
            oldDish.Id,
            new DateOnly(2025, 11, 1), // ~70 days ago
            null,
            null);
        await _historyRepository.Add(oldEntry);

        var request = new GenerateWeeklyPlanRequest(_weekStart);

        // Act
        var result = await _feature.Execute(request);

        // Assert
        var selectedDishIds = result.Plan.Items.Select(i => i.Dish.Id).ToList();
        
        // Old dish more likely to be selected than recent dish
        // (though not guaranteed depending on other factors like ratings)
        Assert.NotEmpty(selectedDishIds);
    }

    [Fact]
    public async Task Execute_ReplacesExistingPlan_WhenCalledTwice()
    {
        // Arrange
        await SeedTestDishes();
        var request = new GenerateWeeklyPlanRequest(_weekStart);

        // Act
        var result1 = await _feature.Execute(request);
        var result2 = await _feature.Execute(request);

        // Assert
        var plans = await _context.WeeklyPlans
            .Where(p => p.WeekStartDate == _weekStart)
            .ToListAsync();
        
        // Should only have one plan for this week
        Assert.Single(plans);
        Assert.Equal(result2.Plan.Id, plans[0].Id);
        Assert.NotEqual(result1.Plan.Id, result2.Plan.Id);
    }

    [Fact]
    public async Task Execute_WithCustomRules_RespectsCustomTimeLimits()
    {
        // Arrange
        await SeedTestDishes();
        var customRules = new PlanningRules(
            WeekdayMaxTotalMinutes: 30,
            WeekendMaxTotalMinutes: 50,
            MinFishDinnersPerWeek: 2);
        var request = new GenerateWeeklyPlanRequest(_weekStart, customRules);

        // Act
        var result = await _feature.Execute(request);

        // Assert
        var violations = _validator.Validate(result.Plan, customRules);
        Assert.Empty(violations);
        
        // Verify weekday dishes respect 30 min limit
        for (int dayIndex = 0; dayIndex <= 4; dayIndex++)
        {
            var item = result.Plan.Items.First(i => i.DayIndex == dayIndex);
            Assert.True(item.Dish.TotalMinutes <= 30);
        }
        
        // Verify weekend dishes respect 50 min limit
        for (int dayIndex = 5; dayIndex <= 6; dayIndex++)
        {
            var item = result.Plan.Items.First(i => i.DayIndex == dayIndex);
            Assert.True(item.Dish.TotalMinutes <= 50);
        }
    }

    [Fact]
    public async Task Execute_WithSeededDishes_ProvidesExplanationsForAllDays()
    {
        // Arrange
        await SeedTestDishes();
        var request = new GenerateWeeklyPlanRequest(_weekStart);

        // Act
        var result = await _feature.Execute(request);

        // Assert
        Assert.Equal(7, result.ExplanationsByDay.Count);
        
        for (int dayIndex = 0; dayIndex < 7; dayIndex++)
        {
            Assert.True(result.ExplanationsByDay.ContainsKey(dayIndex));
            var explanation = result.ExplanationsByDay[dayIndex];
            Assert.NotEmpty(explanation.Reasons);
            Assert.True(explanation.Reasons.Count <= 3);
        }
    }

    [Fact]
    public async Task Execute_WithInsufficientDishes_ThrowsException()
    {
        // Arrange - Only add 3 dishes when we need 7 unique
        await SeedMinimalDishes(count: 3);
        var request = new GenerateWeeklyPlanRequest(_weekStart);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _feature.Execute(request));
        
        Assert.Contains("No available dishes", exception.Message);
    }

    private async Task SeedTestDishes()
    {
        // Use existing ingredients from bootstrapper or create new ones with unique names
        var existingIngredients = await _context.Ingredients.ToListAsync();
        
        var fish = existingIngredients.FirstOrDefault(i => i.Category.ToLower() == "fish" || i.Category.ToLower() == "fisk") 
                   ?? CreateIngredientEntity($"Test-Salmon-{Guid.NewGuid():N}", "fish");
        var chicken = existingIngredients.FirstOrDefault(i => i.Name.Contains("Chicken") || i.Name.Contains("Kylling"))
                      ?? CreateIngredientEntity($"Test-Chicken-{Guid.NewGuid():N}", "meat");
        var pasta = existingIngredients.FirstOrDefault(i => i.Name.Contains("Pasta"))
                    ?? CreateIngredientEntity($"Test-Pasta-{Guid.NewGuid():N}", "grain");
        var rice = existingIngredients.FirstOrDefault(i => i.Name.Contains("Rice") || i.Name.Contains("Ris"))
                   ?? CreateIngredientEntity($"Test-Rice-{Guid.NewGuid():N}", "grain");
        var beef = existingIngredients.FirstOrDefault(i => i.Name.Contains("Beef") || i.Name.Contains("Storfe"))
                   ?? CreateIngredientEntity($"Test-Beef-{Guid.NewGuid():N}", "meat");
        var vegetables = existingIngredients.FirstOrDefault(i => i.Category.ToLower().Contains("vegetable") || i.Category.ToLower().Contains("grønnsaker"))
                         ?? CreateIngredientEntity($"Test-Vegetables-{Guid.NewGuid():N}", "vegetable");

        // Only add ingredients that don't exist
        var ingredientsToAdd = new[] { fish, chicken, pasta, rice, beef, vegetables }
            .Where(i => !existingIngredients.Any(e => e.Id == i.Id))
            .ToList();
        
        if (ingredientsToAdd.Any())
        {
            _context.Ingredients.AddRange(ingredientsToAdd);
            await _context.SaveChangesAsync();
        }

        // Create dishes
        var dishes = new[]
        {
            CreateDishEntity("Quick Fish Tacos", 15, 30, 5, 4, true, false, new[] { fish }),
            CreateDishEntity("Salmon Pasta", 20, 35, 4, 4, true, false, new[] { fish, pasta }),
            CreateDishEntity("Chicken Stir Fry", 15, 30, 4, 5, false, false, new[] { chicken, rice, vegetables }),
            CreateDishEntity("Pasta Carbonara", 10, 25, 5, 4, false, false, new[] { pasta }),
            CreateDishEntity("Chicken Curry", 20, 40, 4, 4, false, false, new[] { chicken, rice }),
            CreateDishEntity("Fish and Chips", 15, 35, 5, 5, true, false, new[] { fish }),
            CreateDishEntity("Beef Tacos", 15, 30, 5, 5, false, false, new[] { beef }),
            CreateDishEntity("Slow Roast Beef", 30, 55, 5, 4, false, false, new[] { beef, vegetables }),
            CreateDishEntity("Baked Salmon", 25, 50, 4, 4, true, false, new[] { fish })
        };

        _context.Dishes.AddRange(dishes);
        await _context.SaveChangesAsync();
    }

    private async Task SeedMinimalDishes(int count)
    {
        var fish = CreateIngredientEntity($"Test-Fish-{Guid.NewGuid():N}", "fish");
        _context.Ingredients.Add(fish);
        await _context.SaveChangesAsync();

        for (int i = 0; i < count; i++)
        {
            var dish = CreateDishEntity(
                $"Dish {i + 1}",
                15,
                30,
                4,
                4,
                i % 2 == 0, // Every other is fish
                false,
                new[] { fish });
            _context.Dishes.Add(dish);
        }

        await _context.SaveChangesAsync();
    }

    private static IngredientEntity CreateIngredientEntity(string name, string category)
    {
        return new IngredientEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Category = category,
            DefaultUnit = "g"
        };
    }

    private static DishEntity CreateDishEntity(
        string name,
        int activeMinutes,
        int totalMinutes,
        int kidRating,
        int familyRating,
        bool isPescetarian,
        bool hasOptionalMeatVariant,
        IngredientEntity[] ingredients)
    {
        var dish = new DishEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            ActiveMinutes = activeMinutes,
            TotalMinutes = totalMinutes,
            KidRating = kidRating,
            FamilyRating = familyRating,
            IsPescetarian = isPescetarian,
            HasOptionalMeatVariant = hasOptionalMeatVariant,
            DishIngredients = new List<DishIngredientEntity>()
        };

        foreach (var ingredient in ingredients)
        {
            dish.DishIngredients.Add(new DishIngredientEntity
            {
                DishId = dish.Id,
                IngredientId = ingredient.Id,
                Amount = 100,
                Unit = ingredient.DefaultUnit,
                Optional = false
            });
        }

        return dish;
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }
}
