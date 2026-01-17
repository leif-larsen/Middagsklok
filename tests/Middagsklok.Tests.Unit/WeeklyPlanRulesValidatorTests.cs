using Middagsklok.Domain;
using Middagsklok.Features.WeeklyPlanning;

namespace Middagsklok.Tests.Unit;

public class WeeklyPlanRulesValidatorTests
{
    private readonly WeeklyPlanRulesValidator _validator = new();

    [Fact]
    public void Validate_WithValidPlan_ReturnsNoViolations()
    {
        // Arrange
        var plan = CreateValidPlan();
        var rules = new PlanningRules();

        // Act
        var violations = _validator.Validate(plan, rules);

        // Assert
        Assert.Empty(violations);
    }

    [Fact]
    public void Validate_WithMissingDay_ReturnsR1Violation()
    {
        // Arrange - 7 items but day 6 is missing (day 5 appears twice instead)
        var items = new[]
        {
            new WeeklyPlanItem(0, CreateDish("Monday", 30)),
            new WeeklyPlanItem(1, CreateDish("Tuesday", 30)),
            new WeeklyPlanItem(2, CreateDish("Wednesday", 30)),
            new WeeklyPlanItem(3, CreateDish("Thursday", 30)),
            new WeeklyPlanItem(4, CreateDish("Friday", 30)),
            new WeeklyPlanItem(5, CreateFishDish("Saturday1", 40)),
            new WeeklyPlanItem(5, CreateFishDish("Saturday2", 40)), // Duplicate day 5, missing day 6
        };
        var plan = new WeeklyPlan(
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.Today),
            DateTimeOffset.UtcNow,
            items);
        var rules = new PlanningRules();

        // Act
        var violations = _validator.Validate(plan, rules);

        // Assert - Should get both R1_MISSING_DAYS (for day 6) and R1_DUPLICATE_DAYS (for day 5)
        var missingViolation = Assert.Single(violations, v => v.RuleCode == "R1_MISSING_DAYS");
        Assert.Contains("6", missingViolation.Message);
        Assert.Single(missingViolation.DayIndices, 6);
        
        var duplicateViolation = Assert.Single(violations, v => v.RuleCode == "R1_DUPLICATE_DAYS");
        Assert.Contains("5", duplicateViolation.Message);
        Assert.Single(duplicateViolation.DayIndices, 5);
    }

    [Fact]
    public void Validate_WithDuplicateDay_ReturnsR1Violation()
    {
        // Arrange
        var plan = CreatePlanWithDays(0, 1, 2, 3, 4, 5, 5); // Duplicate day 5
        var rules = new PlanningRules();

        // Act
        var violations = _validator.Validate(plan, rules);

        // Assert
        var violation = Assert.Single(violations, v => v.RuleCode == "R1_DUPLICATE_DAYS");
        Assert.Contains("5", violation.Message);
        Assert.Single(violation.DayIndices, 5);
    }

    [Fact]
    public void Validate_WithIncompleteWeek_ReturnsR1Violation()
    {
        // Arrange
        var plan = CreatePlanWithDays(0, 1, 2, 3, 4); // Only 5 days
        var rules = new PlanningRules();

        // Act
        var violations = _validator.Validate(plan, rules);

        // Assert
        var violation = Assert.Single(violations, v => v.RuleCode == "R1_INCOMPLETE_WEEK");
        Assert.Contains("exactly 7 items", violation.Message);
        Assert.Contains("5", violation.Message);
    }

    [Fact]
    public void Validate_WithDuplicateDish_ReturnsR2Violation()
    {
        // Arrange
        var dishId = Guid.NewGuid();
        var dish = CreateDish("Taco", 30, dishId);
        var items = new[]
        {
            new WeeklyPlanItem(0, dish),
            new WeeklyPlanItem(1, CreateDish("Pasta", 25)),
            new WeeklyPlanItem(2, CreateDish("Pizza", 20)),
            new WeeklyPlanItem(3, dish), // Duplicate
            new WeeklyPlanItem(4, CreateDish("Burger", 30)),
            new WeeklyPlanItem(5, CreateFishDish("Fish", 40)),
            new WeeklyPlanItem(6, CreateFishDish("Salmon", 50))
        };
        var plan = new WeeklyPlan(
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.Today),
            DateTimeOffset.UtcNow,
            items);
        var rules = new PlanningRules();

        // Act
        var violations = _validator.Validate(plan, rules);

        // Assert
        var violation = Assert.Single(violations, v => v.RuleCode == "R2_DUPLICATE_DISHES");
        Assert.Contains("Taco", violation.Message);
        Assert.Single(violation.DishIds, dishId);
    }

    [Fact]
    public void Validate_WithWeekdayOverTimeLimit_ReturnsR3Violation()
    {
        // Arrange
        var items = new[]
        {
            new WeeklyPlanItem(0, CreateDish("Quick", 30)),
            new WeeklyPlanItem(1, CreateDish("Slow", 60)), // Over 45 min limit
            new WeeklyPlanItem(2, CreateDish("Quick", 30)),
            new WeeklyPlanItem(3, CreateDish("Quick", 30)),
            new WeeklyPlanItem(4, CreateDish("Quick", 30)),
            new WeeklyPlanItem(5, CreateFishDish("Fish", 50)),
            new WeeklyPlanItem(6, CreateFishDish("Salmon", 55))
        };
        var plan = new WeeklyPlan(
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.Today),
            DateTimeOffset.UtcNow,
            items);
        var rules = new PlanningRules();

        // Act
        var violations = _validator.Validate(plan, rules);

        // Assert
        var violation = Assert.Single(violations, v => v.RuleCode == "R3_WEEKDAY_TIME_LIMIT");
        Assert.Contains("45 minutes", violation.Message);
        Assert.Contains("1", violation.Message);
        Assert.Single(violation.DayIndices, 1);
    }

    [Fact]
    public void Validate_WithWeekendOverTimeLimit_ReturnsR3Violation()
    {
        // Arrange
        var items = new[]
        {
            new WeeklyPlanItem(0, CreateDish("Quick", 30)),
            new WeeklyPlanItem(1, CreateDish("Quick", 30)),
            new WeeklyPlanItem(2, CreateDish("Quick", 30)),
            new WeeklyPlanItem(3, CreateDish("Quick", 30)),
            new WeeklyPlanItem(4, CreateDish("Quick", 30)),
            new WeeklyPlanItem(5, CreateFishDish("Fish", 50)),
            new WeeklyPlanItem(6, CreateFishDish("Salmon", 75)) // Over 60 min limit
        };
        var plan = new WeeklyPlan(
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.Today),
            DateTimeOffset.UtcNow,
            items);
        var rules = new PlanningRules();

        // Act
        var violations = _validator.Validate(plan, rules);

        // Assert
        var violation = Assert.Single(violations, v => v.RuleCode == "R3_WEEKEND_TIME_LIMIT");
        Assert.Contains("60 minutes", violation.Message);
        Assert.Contains("6", violation.Message);
        Assert.Single(violation.DayIndices, 6);
    }

    [Fact]
    public void Validate_WithInsufficientFish_ReturnsR4Violation()
    {
        // Arrange
        var items = new[]
        {
            new WeeklyPlanItem(0, CreateDish("Taco", 30)),
            new WeeklyPlanItem(1, CreateDish("Pasta", 25)),
            new WeeklyPlanItem(2, CreateDish("Pizza", 20)),
            new WeeklyPlanItem(3, CreateDish("Burger", 30)),
            new WeeklyPlanItem(4, CreateDish("Chicken", 35)),
            new WeeklyPlanItem(5, CreateFishDish("Fish", 40)), // Only 1 fish dish
            new WeeklyPlanItem(6, CreateDish("Steak", 50))
        };
        var plan = new WeeklyPlan(
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.Today),
            DateTimeOffset.UtcNow,
            items);
        var rules = new PlanningRules();

        // Act
        var violations = _validator.Validate(plan, rules);

        // Assert
        var violation = Assert.Single(violations, v => v.RuleCode == "R4_INSUFFICIENT_FISH");
        Assert.Contains("at least 2 fish dinners", violation.Message);
        Assert.Contains("1", violation.Message);
    }

    [Fact]
    public void Validate_WithFishDetectedByIsPescetarian_CountsAsFish()
    {
        // Arrange
        var items = new[]
        {
            new WeeklyPlanItem(0, CreateDish("Taco", 30)),
            new WeeklyPlanItem(1, CreateDish("Pasta", 25)),
            new WeeklyPlanItem(2, CreateDish("Pizza", 20)),
            new WeeklyPlanItem(3, CreateDish("Burger", 30)),
            new WeeklyPlanItem(4, CreateDish("Chicken", 35)),
            new WeeklyPlanItem(5, CreatePescetarianDish("Shrimp", 40)), // Pescetarian = fish
            new WeeklyPlanItem(6, CreateFishDish("Salmon", 50)) // Has fish ingredient
        };
        var plan = new WeeklyPlan(
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.Today),
            DateTimeOffset.UtcNow,
            items);
        var rules = new PlanningRules();

        // Act
        var violations = _validator.Validate(plan, rules);

        // Assert
        Assert.DoesNotContain(violations, v => v.RuleCode == "R4_INSUFFICIENT_FISH");
    }

    [Fact]
    public void Validate_WithFishDetectedByIngredientCategory_CountsAsFish()
    {
        // Arrange
        var items = new[]
        {
            new WeeklyPlanItem(0, CreateDish("Taco", 30)),
            new WeeklyPlanItem(1, CreateDish("Pasta", 25)),
            new WeeklyPlanItem(2, CreateDish("Pizza", 20)),
            new WeeklyPlanItem(3, CreateDish("Burger", 30)),
            new WeeklyPlanItem(4, CreateDish("Chicken", 35)),
            new WeeklyPlanItem(5, CreateFishDish("Fish Tacos", 40)),
            new WeeklyPlanItem(6, CreateFishDish("Salmon", 50))
        };
        var plan = new WeeklyPlan(
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.Today),
            DateTimeOffset.UtcNow,
            items);
        var rules = new PlanningRules();

        // Act
        var violations = _validator.Validate(plan, rules);

        // Assert
        Assert.DoesNotContain(violations, v => v.RuleCode == "R4_INSUFFICIENT_FISH");
    }

    [Fact]
    public void Validate_WithMixedFishDetection_CountsCorrectly()
    {
        // Arrange - one via IsPescetarian, one via ingredient, one via both
        var items = new[]
        {
            new WeeklyPlanItem(0, CreateDish("Taco", 30)),
            new WeeklyPlanItem(1, CreateDish("Pasta", 25)),
            new WeeklyPlanItem(2, CreateDish("Pizza", 20)),
            new WeeklyPlanItem(3, CreateDish("Burger", 30)),
            new WeeklyPlanItem(4, CreatePescetarianDish("Shrimp Salad", 35)), // Via flag
            new WeeklyPlanItem(5, CreateFishDish("Cod", 40)), // Via ingredient
            new WeeklyPlanItem(6, CreatePescetarianFishDish("Salmon", 50)) // Both
        };
        var plan = new WeeklyPlan(
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.Today),
            DateTimeOffset.UtcNow,
            items);
        var rules = new PlanningRules();

        // Act
        var violations = _validator.Validate(plan, rules);

        // Assert - Should have 3 fish dishes, no violation
        Assert.DoesNotContain(violations, v => v.RuleCode == "R4_INSUFFICIENT_FISH");
    }

    // Helper methods
    private WeeklyPlan CreateValidPlan()
    {
        var items = new[]
        {
            new WeeklyPlanItem(0, CreateDish("Monday", 30)),
            new WeeklyPlanItem(1, CreateDish("Tuesday", 30)),
            new WeeklyPlanItem(2, CreateDish("Wednesday", 30)),
            new WeeklyPlanItem(3, CreateDish("Thursday", 30)),
            new WeeklyPlanItem(4, CreateDish("Friday", 30)),
            new WeeklyPlanItem(5, CreateFishDish("Saturday", 50)),
            new WeeklyPlanItem(6, CreateFishDish("Sunday", 55))
        };

        return new WeeklyPlan(
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.Today),
            DateTimeOffset.UtcNow,
            items);
    }

    private WeeklyPlan CreatePlanWithDays(params int[] dayIndices)
    {
        var items = dayIndices.Select((day, index) =>
        {
            var isFish = day >= 5; // Make last 2 days fish to avoid R4 violations
            var dish = isFish ? CreateFishDish($"Dish{index}", 40) : CreateDish($"Dish{index}", 30);
            return new WeeklyPlanItem(day, dish);
        }).ToArray();

        return new WeeklyPlan(
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.Today),
            DateTimeOffset.UtcNow,
            items);
    }

    private Dish CreateDish(string name, int totalMinutes, Guid? id = null)
    {
        var ingredient = new Ingredient(Guid.NewGuid(), name + " ingredient", "general", "unit");
        var dishIngredient = new DishIngredient(ingredient, 1, "unit", false);

        return new Dish(
            id ?? Guid.NewGuid(),
            name,
            totalMinutes / 2,
            totalMinutes,
            3,
            4,
            false,
            false,
            new[] { dishIngredient });
    }

    private Dish CreateFishDish(string name, int totalMinutes)
    {
        var fishIngredient = new Ingredient(Guid.NewGuid(), name + " fish", "fish", "unit");
        var dishIngredient = new DishIngredient(fishIngredient, 1, "unit", false);

        return new Dish(
            Guid.NewGuid(),
            name,
            totalMinutes / 2,
            totalMinutes,
            3,
            4,
            false,
            false,
            new[] { dishIngredient });
    }

    private Dish CreatePescetarianDish(string name, int totalMinutes)
    {
        var ingredient = new Ingredient(Guid.NewGuid(), name + " ingredient", "seafood", "unit");
        var dishIngredient = new DishIngredient(ingredient, 1, "unit", false);

        return new Dish(
            Guid.NewGuid(),
            name,
            totalMinutes / 2,
            totalMinutes,
            3,
            4,
            true, // IsPescetarian = true
            false,
            new[] { dishIngredient });
    }

    private Dish CreatePescetarianFishDish(string name, int totalMinutes)
    {
        var fishIngredient = new Ingredient(Guid.NewGuid(), name + " fish", "fish", "unit");
        var dishIngredient = new DishIngredient(fishIngredient, 1, "unit", false);

        return new Dish(
            Guid.NewGuid(),
            name,
            totalMinutes / 2,
            totalMinutes,
            3,
            4,
            true, // IsPescetarian = true AND has fish ingredient
            false,
            new[] { dishIngredient });
    }
}
