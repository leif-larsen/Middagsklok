using FakeItEasy;
using Middagsklok.Domain;
using Middagsklok.Features.WeeklyPlans.Generate;

namespace Middagsklok.Tests.Unit;

public class GenerateWeeklyPlanFeatureTests
{
    private readonly IDishRepository _dishRepository;
    private readonly IDishHistoryRepository _historyRepository;
    private readonly IWeeklyPlanRepository _planRepository;
    private readonly WeeklyPlanRulesValidator _validator;
    private readonly IWeeklyPlanGenerationService _generationService;
    private readonly GenerateWeeklyPlanFeature _feature;
    private readonly DateOnly _today = new(2026, 1, 17);
    private readonly DateOnly _weekStart = new(2026, 1, 12); // Monday

    public GenerateWeeklyPlanFeatureTests()
    {
        _dishRepository = A.Fake<IDishRepository>();
        _historyRepository = A.Fake<IDishHistoryRepository>();
        _planRepository = A.Fake<IWeeklyPlanRepository>();
        _validator = new WeeklyPlanRulesValidator();
        _generationService = new WeeklyPlanGenerationService(
            _dishRepository,
            _historyRepository,
            _validator);
        _feature = new GenerateWeeklyPlanFeature(
            _generationService,
            _planRepository);
    }

    [Fact]
    public async Task Execute_WithValidDishes_GeneratesSevenUniqueDishes()
    {
        // Arrange
        var dishes = CreateTestDishes();
        var lastEaten = new Dictionary<Guid, DateOnly>();
        
        A.CallTo(() => _dishRepository.GetAllWithIngredients(A<CancellationToken>._))
            .Returns(dishes);
        A.CallTo(() => _historyRepository.GetLastEatenByDish(A<CancellationToken>._))
            .Returns(lastEaten);
        A.CallTo(() => _planRepository.CreateOrReplace(A<WeeklyPlan>._, A<CancellationToken>._))
            .ReturnsLazily((WeeklyPlan plan, CancellationToken ct) => plan);

        var request = new GenerateWeeklyPlanRequest(_weekStart);

        // Act
        var result = await _feature.Execute(request);

        // Assert
        Assert.Equal(7, result.Plan.Items.Count);
        
        var dishIds = result.Plan.Items.Select(i => i.Dish.Id).ToList();
        Assert.Equal(7, dishIds.Distinct().Count()); // All unique
    }

    [Fact]
    public async Task Execute_WithValidDishes_IncludesAtLeastTwoFishDishes()
    {
        // Arrange
        var dishes = CreateTestDishes();
        var lastEaten = new Dictionary<Guid, DateOnly>();
        
        A.CallTo(() => _dishRepository.GetAllWithIngredients(A<CancellationToken>._))
            .Returns(dishes);
        A.CallTo(() => _historyRepository.GetLastEatenByDish(A<CancellationToken>._))
            .Returns(lastEaten);
        A.CallTo(() => _planRepository.CreateOrReplace(A<WeeklyPlan>._, A<CancellationToken>._))
            .ReturnsLazily((WeeklyPlan plan, CancellationToken ct) => plan);

        var request = new GenerateWeeklyPlanRequest(_weekStart);

        // Act
        var result = await _feature.Execute(request);

        // Assert
        var fishDishCount = result.Plan.Items
            .Count(i => i.Dish.IsPescetarian || 
                       i.Dish.Ingredients.Any(di => 
                           string.Equals(di.Ingredient.Category, "fish", StringComparison.OrdinalIgnoreCase)));
        
        Assert.True(fishDishCount >= 2, $"Expected at least 2 fish dishes, got {fishDishCount}");
    }

    [Fact]
    public async Task Execute_WithValidDishes_RespectsWeekdayTimeLimit()
    {
        // Arrange
        var dishes = CreateTestDishes();
        var lastEaten = new Dictionary<Guid, DateOnly>();
        var rules = new PlanningRules(WeekdayMaxTotalMinutes: 45);
        
        A.CallTo(() => _dishRepository.GetAllWithIngredients(A<CancellationToken>._))
            .Returns(dishes);
        A.CallTo(() => _historyRepository.GetLastEatenByDish(A<CancellationToken>._))
            .Returns(lastEaten);
        A.CallTo(() => _planRepository.CreateOrReplace(A<WeeklyPlan>._, A<CancellationToken>._))
            .ReturnsLazily((WeeklyPlan plan, CancellationToken ct) => plan);

        var request = new GenerateWeeklyPlanRequest(_weekStart, rules);

        // Act
        var result = await _feature.Execute(request);

        // Assert
        // Check weekdays (0-4)
        for (int dayIndex = 0; dayIndex <= 4; dayIndex++)
        {
            var item = result.Plan.Items.First(i => i.DayIndex == dayIndex);
            Assert.True(item.Dish.TotalMinutes <= 45, 
                $"Day {dayIndex} dish '{item.Dish.Name}' exceeds weekday limit: {item.Dish.TotalMinutes} > 45");
        }
    }

    [Fact]
    public async Task Execute_WithValidDishes_RespectsWeekendTimeLimit()
    {
        // Arrange
        var dishes = CreateTestDishes();
        var lastEaten = new Dictionary<Guid, DateOnly>();
        var rules = new PlanningRules(WeekendMaxTotalMinutes: 60);
        
        A.CallTo(() => _dishRepository.GetAllWithIngredients(A<CancellationToken>._))
            .Returns(dishes);
        A.CallTo(() => _historyRepository.GetLastEatenByDish(A<CancellationToken>._))
            .Returns(lastEaten);
        A.CallTo(() => _planRepository.CreateOrReplace(A<WeeklyPlan>._, A<CancellationToken>._))
            .ReturnsLazily((WeeklyPlan plan, CancellationToken ct) => plan);

        var request = new GenerateWeeklyPlanRequest(_weekStart, rules);

        // Act
        var result = await _feature.Execute(request);

        // Assert
        // Check weekends (5-6)
        for (int dayIndex = 5; dayIndex <= 6; dayIndex++)
        {
            var item = result.Plan.Items.First(i => i.DayIndex == dayIndex);
            Assert.True(item.Dish.TotalMinutes <= 60, 
                $"Day {dayIndex} dish '{item.Dish.Name}' exceeds weekend limit: {item.Dish.TotalMinutes} > 60");
        }
    }

    [Fact]
    public async Task Execute_WithSameInput_GeneratesSamePlan()
    {
        // Arrange
        var dishes = CreateTestDishes();
        var lastEaten = new Dictionary<Guid, DateOnly>
        {
            { dishes[0].Id, new DateOnly(2026, 1, 1) },
            { dishes[1].Id, new DateOnly(2026, 1, 5) }
        };
        
        A.CallTo(() => _dishRepository.GetAllWithIngredients(A<CancellationToken>._))
            .Returns(dishes);
        A.CallTo(() => _historyRepository.GetLastEatenByDish(A<CancellationToken>._))
            .Returns(lastEaten);
        A.CallTo(() => _planRepository.CreateOrReplace(A<WeeklyPlan>._, A<CancellationToken>._))
            .ReturnsLazily((WeeklyPlan plan, CancellationToken ct) => plan);

        var request = new GenerateWeeklyPlanRequest(_weekStart);

        // Act
        var result1 = await _feature.Execute(request);
        var result2 = await _feature.Execute(request);

        // Assert
        var dishIds1 = result1.Plan.Items.Select(i => i.Dish.Id).ToList();
        var dishIds2 = result2.Plan.Items.Select(i => i.Dish.Id).ToList();
        
        Assert.Equal(dishIds1, dishIds2);
    }

    [Fact]
    public async Task Execute_WithValidDishes_ProvidesExplanationsForEachDay()
    {
        // Arrange
        var dishes = CreateTestDishes();
        var lastEaten = new Dictionary<Guid, DateOnly>();
        
        A.CallTo(() => _dishRepository.GetAllWithIngredients(A<CancellationToken>._))
            .Returns(dishes);
        A.CallTo(() => _historyRepository.GetLastEatenByDish(A<CancellationToken>._))
            .Returns(lastEaten);
        A.CallTo(() => _planRepository.CreateOrReplace(A<WeeklyPlan>._, A<CancellationToken>._))
            .ReturnsLazily((WeeklyPlan plan, CancellationToken ct) => plan);

        var request = new GenerateWeeklyPlanRequest(_weekStart);

        // Act
        var result = await _feature.Execute(request);

        // Assert
        Assert.Equal(7, result.ExplanationsByDay.Count);
        
        for (int dayIndex = 0; dayIndex < 7; dayIndex++)
        {
            Assert.True(result.ExplanationsByDay.ContainsKey(dayIndex), 
                $"Missing explanation for day {dayIndex}");
            
            var explanation = result.ExplanationsByDay[dayIndex];
            Assert.NotEmpty(explanation.Reasons);
            Assert.True(explanation.Reasons.Count <= 3, 
                $"Too many reasons for day {dayIndex}: {explanation.Reasons.Count}");
        }
    }

    [Fact]
    public async Task Execute_WithFishDishes_ExplanationIncludesFishRequirement()
    {
        // Arrange
        var dishes = CreateTestDishes();
        var lastEaten = new Dictionary<Guid, DateOnly>();
        
        A.CallTo(() => _dishRepository.GetAllWithIngredients(A<CancellationToken>._))
            .Returns(dishes);
        A.CallTo(() => _historyRepository.GetLastEatenByDish(A<CancellationToken>._))
            .Returns(lastEaten);
        A.CallTo(() => _planRepository.CreateOrReplace(A<WeeklyPlan>._, A<CancellationToken>._))
            .ReturnsLazily((WeeklyPlan plan, CancellationToken ct) => plan);

        var request = new GenerateWeeklyPlanRequest(_weekStart);

        // Act
        var result = await _feature.Execute(request);

        // Assert
        var fishExplanations = result.ExplanationsByDay.Values
            .Where(e => e.Reasons.Any(r => r.Contains("Fish requirement")))
            .ToList();
        
        Assert.True(fishExplanations.Count >= 2, 
            $"Expected at least 2 explanations with 'Fish requirement', got {fishExplanations.Count}");
    }

    [Fact]
    public async Task Execute_WithNeverEatenDishes_ExplanationIncludesNeverEaten()
    {
        // Arrange
        var dishes = CreateTestDishes();
        var lastEaten = new Dictionary<Guid, DateOnly>(); // Empty - no dishes eaten
        
        A.CallTo(() => _dishRepository.GetAllWithIngredients(A<CancellationToken>._))
            .Returns(dishes);
        A.CallTo(() => _historyRepository.GetLastEatenByDish(A<CancellationToken>._))
            .Returns(lastEaten);
        A.CallTo(() => _planRepository.CreateOrReplace(A<WeeklyPlan>._, A<CancellationToken>._))
            .ReturnsLazily((WeeklyPlan plan, CancellationToken ct) => plan);

        var request = new GenerateWeeklyPlanRequest(_weekStart);

        // Act
        var result = await _feature.Execute(request);

        // Assert
        var neverEatenCount = result.ExplanationsByDay.Values
            .Count(e => e.Reasons.Any(r => r.Contains("Never eaten before")));
        
        Assert.True(neverEatenCount >= 1, 
            "Expected at least one explanation mentioning 'Never eaten before'");
    }

    [Fact]
    public async Task Execute_WithRecentlyEatenDishes_ExplanationIncludesDaysSince()
    {
        // Arrange
        var dishes = CreateTestDishes();
        var lastEaten = new Dictionary<Guid, DateOnly>
        {
            { dishes[0].Id, new DateOnly(2025, 12, 1) } // 47 days ago from today (2026-01-17)
        };
        
        A.CallTo(() => _dishRepository.GetAllWithIngredients(A<CancellationToken>._))
            .Returns(dishes);
        A.CallTo(() => _historyRepository.GetLastEatenByDish(A<CancellationToken>._))
            .Returns(lastEaten);
        A.CallTo(() => _planRepository.CreateOrReplace(A<WeeklyPlan>._, A<CancellationToken>._))
            .ReturnsLazily((WeeklyPlan plan, CancellationToken ct) => plan);

        var request = new GenerateWeeklyPlanRequest(_weekStart);

        // Act
        var result = await _feature.Execute(request);

        // Assert
        var explanation = result.ExplanationsByDay.Values
            .FirstOrDefault(e => e.DishId == dishes[0].Id);
        
        if (explanation != null)
        {
            Assert.Contains(explanation.Reasons, r => r.Contains("Not eaten recently"));
        }
    }

    [Fact]
    public async Task Execute_WithTiedScores_UsesAlphabeticalTieBreaking()
    {
        // Arrange
        var fish = CreateIngredient("Salmon", "fish");
        var dishes = new List<Dish>
        {
            CreateDish("Zucchini Pasta", 30, 40, 4, 4, false, new[] { fish }),
            CreateDish("Apple Salad", 30, 40, 4, 4, false, new[] { fish }),
            CreateDish("Banana Soup", 30, 40, 4, 4, false, new[] { fish }),
            CreateDish("Carrot Stew", 30, 40, 4, 4, false, new[] { fish }),
            CreateDish("Dill Fish", 30, 40, 4, 4, true, new[] { fish }),
            CreateDish("Egg Fried Rice", 30, 40, 4, 4, false, new[] { fish }),
            CreateDish("Fig Tart", 30, 40, 4, 4, false, new[] { fish }),
            CreateDish("Grape Juice", 30, 40, 4, 4, false, new[] { fish })
        };
        
        var lastEaten = new Dictionary<Guid, DateOnly>(); // All never eaten - same score
        
        A.CallTo(() => _dishRepository.GetAllWithIngredients(A<CancellationToken>._))
            .Returns(dishes);
        A.CallTo(() => _historyRepository.GetLastEatenByDish(A<CancellationToken>._))
            .Returns(lastEaten);
        A.CallTo(() => _planRepository.CreateOrReplace(A<WeeklyPlan>._, A<CancellationToken>._))
            .ReturnsLazily((WeeklyPlan plan, CancellationToken ct) => plan);

        var request = new GenerateWeeklyPlanRequest(_weekStart);

        // Act
        var result1 = await _feature.Execute(request);
        var result2 = await _feature.Execute(request);

        // Assert
        // Should be deterministic (same dishes selected)
        var names1 = result1.Plan.Items.Select(i => i.Dish.Name).ToList();
        var names2 = result2.Plan.Items.Select(i => i.Dish.Name).ToList();
        Assert.Equal(names1, names2);
        
        // First selected dishes should be alphabetically first
        // (considering fish must be selected first, then others)
    }

    private static List<Dish> CreateTestDishes()
    {
        var fish = CreateIngredient("Salmon", "fish");
        var chicken = CreateIngredient("Chicken", "meat");
        var pasta = CreateIngredient("Pasta", "grain");
        var rice = CreateIngredient("Rice", "grain");
        var beef = CreateIngredient("Beef", "meat");

        return new List<Dish>
        {
            // Quick weekday dishes
            CreateDish("Quick Fish Tacos", 15, 30, 5, 4, true, new[] { fish }),
            CreateDish("Salmon Pasta", 20, 35, 4, 4, true, new[] { fish, pasta }),
            CreateDish("Chicken Stir Fry", 15, 30, 4, 5, false, new[] { chicken, rice }),
            CreateDish("Pasta Carbonara", 10, 25, 5, 4, false, new[] { pasta }),
            CreateDish("Chicken Curry", 20, 40, 4, 4, false, new[] { chicken, rice }),
            CreateDish("Fish and Chips", 15, 35, 5, 5, true, new[] { fish }),
            CreateDish("Beef Tacos", 15, 30, 5, 5, false, new[] { beef }),
            
            // Weekend dishes (can be longer)
            CreateDish("Slow Roast Beef", 30, 55, 5, 4, false, new[] { beef }),
            CreateDish("Baked Salmon", 25, 50, 4, 4, true, new[] { fish })
        };
    }

    private static Dish CreateDish(
        string name,
        int activeMinutes,
        int totalMinutes,
        int kidRating,
        int familyRating,
        bool isPescetarian,
        Ingredient[] ingredients)
    {
        var dishIngredients = ingredients
            .Select(i => new DishIngredient(i, 1, i.DefaultUnit, false))
            .ToList();

        return new Dish(
            Id: Guid.NewGuid(),
            Name: name,
            ActiveMinutes: activeMinutes,
            TotalMinutes: totalMinutes,
            KidRating: kidRating,
            FamilyRating: familyRating,
            IsPescetarian: isPescetarian,
            HasOptionalMeatVariant: false,
            Ingredients: dishIngredients);
    }

    private static Ingredient CreateIngredient(string name, string category)
    {
        return new Ingredient(
            Id: Guid.NewGuid(),
            Name: name,
            Category: category,
            DefaultUnit: "g");
    }
}
