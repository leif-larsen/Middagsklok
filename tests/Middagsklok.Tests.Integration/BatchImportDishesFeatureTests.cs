using Microsoft.EntityFrameworkCore;
using Middagsklok.Database;
using Middagsklok.Database.Repositories;
using Middagsklok.Features.Dishes.Import;

namespace Middagsklok.Tests.Integration;

public class BatchImportDishesFeatureTests : IDisposable
{
    private readonly MiddagsklokDbContext _context;
    private readonly DishImportRepository _importRepository;
    private readonly BatchImportDishesFeature _feature;

    public BatchImportDishesFeatureTests()
    {
        var options = new DbContextOptionsBuilder<MiddagsklokDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new MiddagsklokDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _importRepository = new DishImportRepository(_context);
        _feature = new BatchImportDishesFeature(_importRepository);
    }

    [Fact]
    public async Task Execute_ImportsMultipleDishes()
    {
        // Arrange
        var command = new BatchImportDishesCommand(
        [
            new AddDishCommand(
                Name: "Laks med ovnsgrønnsaker",
                ActiveMinutes: 15,
                TotalMinutes: 35,
                KidRating: 4,
                FamilyRating: 5,
                IsPescetarian: true,
                HasOptionalMeatVariant: false,
                Tags: null,
                Ingredients:
                [
                    new AddDishIngredientItem("laks", "fish", 600, "g", false),
                    new AddDishIngredientItem("potet", "produce", 800, "g", false)
                ]),
            new AddDishCommand(
                Name: "Veggie taco",
                ActiveMinutes: 20,
                TotalMinutes: 35,
                KidRating: 4,
                FamilyRating: 4,
                IsPescetarian: true,
                HasOptionalMeatVariant: true,
                Tags: null,
                Ingredients:
                [
                    new AddDishIngredientItem("tortilla", "dry", 8, "stk", false),
                    new AddDishIngredientItem("bønner", "dry", 2, "boks", false)
                ])
        ]);

        // Act
        var result = await _feature.Execute(command);

        // Assert
        Assert.Equal(2, result.Total);
        Assert.Equal(2, result.Created);
        Assert.Equal(0, result.Skipped);
        Assert.Equal(0, result.Failed);

        Assert.All(result.Results, r =>
        {
            Assert.Equal("created", r.Status);
            Assert.NotNull(r.DishId);
            Assert.Null(r.Error);
        });

        // Verify in database
        var dishes = await _context.Dishes.Include(d => d.DishIngredients).ToListAsync();
        Assert.Equal(2, dishes.Count);

        var laks = dishes.First(d => d.Name == "Laks med ovnsgrønnsaker");
        Assert.Equal(2, laks.DishIngredients.Count);

        var taco = dishes.First(d => d.Name == "Veggie taco");
        Assert.Equal(2, taco.DishIngredients.Count);
    }

    [Fact]
    public async Task Execute_SkipsExistingDish_CaseInsensitive()
    {
        // Arrange - Create existing dish
        var firstCommand = new BatchImportDishesCommand(
        [
            new AddDishCommand(
                Name: "Pasta Carbonara",
                ActiveMinutes: 20,
                TotalMinutes: 30,
                KidRating: 5,
                FamilyRating: 5,
                IsPescetarian: true,
                HasOptionalMeatVariant: false,
                Tags: null,
                Ingredients:
                [
                    new AddDishIngredientItem("pasta", "dry", 400, "g", false)
                ])
        ]);

        var firstResult = await _feature.Execute(firstCommand);
        var existingDishId = firstResult.Results[0].DishId;

        // Try to import same dish with different casing
        var secondCommand = new BatchImportDishesCommand(
        [
            new AddDishCommand(
                Name: "PASTA CARBONARA",
                ActiveMinutes: 25,
                TotalMinutes: 35,
                KidRating: 4,
                FamilyRating: 4,
                IsPescetarian: true,
                HasOptionalMeatVariant: false,
                Tags: null,
                Ingredients:
                [
                    new AddDishIngredientItem("pasta", "dry", 500, "g", false)
                ])
        ]);

        // Act
        var result = await _feature.Execute(secondCommand);

        // Assert
        Assert.Equal(1, result.Total);
        Assert.Equal(0, result.Created);
        Assert.Equal(1, result.Skipped);
        Assert.Equal(0, result.Failed);

        var skipped = result.Results[0];
        Assert.Equal("skipped", skipped.Status);
        Assert.Equal(existingDishId, skipped.DishId);
        Assert.Null(skipped.Error);

        // Verify only one dish exists
        var dishes = await _context.Dishes.ToListAsync();
        Assert.Single(dishes);
    }

    [Fact]
    public async Task Execute_ContinuesOnError_OtherDishesCreated()
    {
        // Arrange
        var command = new BatchImportDishesCommand(
        [
            new AddDishCommand(
                Name: "Valid Dish",
                ActiveMinutes: 20,
                TotalMinutes: 30,
                KidRating: 4,
                FamilyRating: 5,
                IsPescetarian: true,
                HasOptionalMeatVariant: false,
                Tags: null,
                Ingredients:
                [
                    new AddDishIngredientItem("ingredient", "produce", 100, "g", false)
                ]),
            new AddDishCommand(
                Name: "Invalid Dish",
                ActiveMinutes: 20,
                TotalMinutes: 30,
                KidRating: 99, // Invalid rating
                FamilyRating: 5,
                IsPescetarian: true,
                HasOptionalMeatVariant: false,
                Tags: null,
                Ingredients:
                [
                    new AddDishIngredientItem("ingredient", "produce", 100, "g", false)
                ]),
            new AddDishCommand(
                Name: "Another Valid Dish",
                ActiveMinutes: 15,
                TotalMinutes: 25,
                KidRating: 3,
                FamilyRating: 4,
                IsPescetarian: true,
                HasOptionalMeatVariant: false,
                Tags: null,
                Ingredients:
                [
                    new AddDishIngredientItem("ingredient2", "produce", 200, "g", false)
                ])
        ]);

        // Act
        var result = await _feature.Execute(command);

        // Assert
        Assert.Equal(3, result.Total);
        Assert.Equal(2, result.Created);
        Assert.Equal(0, result.Skipped);
        Assert.Equal(1, result.Failed);

        var failed = result.Results.First(r => r.Status == "failed");
        Assert.Equal("Invalid Dish", failed.Name);
        Assert.Null(failed.DishId);
        Assert.NotNull(failed.Error);

        // Verify two dishes were created
        var dishes = await _context.Dishes.ToListAsync();
        Assert.Equal(2, dishes.Count);
    }

    [Fact]
    public async Task Execute_UpsertsSharedIngredient()
    {
        // Arrange - Two dishes sharing the same ingredient
        var command = new BatchImportDishesCommand(
        [
            new AddDishCommand(
                Name: "Pasta Bolognese",
                ActiveMinutes: 30,
                TotalMinutes: 45,
                KidRating: 5,
                FamilyRating: 5,
                IsPescetarian: false,
                HasOptionalMeatVariant: false,
                Tags: null,
                Ingredients:
                [
                    new AddDishIngredientItem("pasta", "dry", 400, "g", false),
                    new AddDishIngredientItem("tomat", "produce", 400, "g", false)
                ]),
            new AddDishCommand(
                Name: "Pasta Arrabiata",
                ActiveMinutes: 20,
                TotalMinutes: 30,
                KidRating: 4,
                FamilyRating: 4,
                IsPescetarian: true,
                HasOptionalMeatVariant: false,
                Tags: null,
                Ingredients:
                [
                    new AddDishIngredientItem("pasta", "dry", 400, "g", false),
                    new AddDishIngredientItem("chili", "produce", 2, "stk", false)
                ])
        ]);

        // Act
        var result = await _feature.Execute(command);

        // Assert
        Assert.Equal(2, result.Created);

        // Verify ingredients are not duplicated
        var ingredients = await _context.Ingredients.ToListAsync();
        var pastaIngredients = ingredients.Where(i => i.Name.ToLower() == "pasta").ToList();
        Assert.Single(pastaIngredients);

        // Verify both dishes use the same ingredient entity
        var dishes = await _context.Dishes
            .Include(d => d.DishIngredients)
            .ThenInclude(di => di.Ingredient)
            .ToListAsync();

        var pasta1 = dishes.First(d => d.Name == "Pasta Bolognese").DishIngredients
            .First(di => di.Ingredient.Name.ToLower() == "pasta");
        var pasta2 = dishes.First(d => d.Name == "Pasta Arrabiata").DishIngredients
            .First(di => di.Ingredient.Name.ToLower() == "pasta");

        Assert.Equal(pasta1.IngredientId, pasta2.IngredientId);
    }

    [Fact]
    public async Task Execute_AggregatesDuplicateIngredientsWithinDish()
    {
        // Arrange - Dish with duplicate ingredient lines
        var command = new BatchImportDishesCommand(
        [
            new AddDishCommand(
                Name: "Complex Salad",
                ActiveMinutes: 15,
                TotalMinutes: 15,
                KidRating: 3,
                FamilyRating: 4,
                IsPescetarian: true,
                HasOptionalMeatVariant: false,
                Tags: null,
                Ingredients:
                [
                    new AddDishIngredientItem("tomat", "produce", 100, "g", false),
                    new AddDishIngredientItem("agurk", "produce", 50, "g", false),
                    new AddDishIngredientItem("tomat", "produce", 50, "g", false), // Duplicate
                    new AddDishIngredientItem("ost", "dairy", 100, "g", true),
                    new AddDishIngredientItem("ost", "dairy", 50, "g", true) // Duplicate
                ])
        ]);

        // Act
        var result = await _feature.Execute(command);

        // Assert
        Assert.Equal(1, result.Created);

        var dish = await _context.Dishes
            .Include(d => d.DishIngredients)
            .ThenInclude(di => di.Ingredient)
            .FirstAsync();

        // Should have 3 dish_ingredient rows (tomat aggregated, ost aggregated, agurk)
        Assert.Equal(3, dish.DishIngredients.Count);

        var tomatLine = dish.DishIngredients.First(di => di.Ingredient.Name.ToLower() == "tomat");
        Assert.Equal(150, tomatLine.Amount); // 100 + 50

        var ostLine = dish.DishIngredients.First(di => di.Ingredient.Name.ToLower() == "ost");
        Assert.Equal(150, ostLine.Amount); // 100 + 50

        var agurkLine = dish.DishIngredients.First(di => di.Ingredient.Name.ToLower() == "agurk");
        Assert.Equal(50, agurkLine.Amount);
    }

    [Fact]
    public async Task Execute_ThrowsWhenNoDishes()
    {
        // Arrange
        var command = new BatchImportDishesCommand([]);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _feature.Execute(command));
    }

    [Fact]
    public async Task Execute_ValidatesRatings()
    {
        // Arrange
        var command = new BatchImportDishesCommand(
        [
            new AddDishCommand(
                Name: "Test Dish",
                ActiveMinutes: 20,
                TotalMinutes: 30,
                KidRating: 0, // Invalid
                FamilyRating: 5,
                IsPescetarian: true,
                HasOptionalMeatVariant: false,
                Tags: null,
                Ingredients:
                [
                    new AddDishIngredientItem("ingredient", "produce", 100, "g", false)
                ])
        ]);

        // Act
        var result = await _feature.Execute(command);

        // Assert
        Assert.Equal(1, result.Failed);
        Assert.Contains("rating", result.Results[0].Error, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
