using Microsoft.EntityFrameworkCore;
using Middagsklok.Database;
using Middagsklok.Database.Repositories;
using Middagsklok.Features.Dishes.UpdateDish;

namespace Middagsklok.Tests.Integration;

public class UpdateDishFeatureTests : IDisposable
{
    private readonly MiddagsklokDbContext _context;
    private readonly DishRepository _dishRepository;
    private readonly DishUpdateRepository _updateRepository;
    private readonly UpdateDishFeature _feature;

    public UpdateDishFeatureTests()
    {
        var options = new DbContextOptionsBuilder<MiddagsklokDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new MiddagsklokDbContext(options);
        _context.Database.OpenConnection();
        
        var bootstrapper = new DbBootstrapper(_context);
        bootstrapper.InitializeAsync().GetAwaiter().GetResult();

        _dishRepository = new DishRepository(_context);
        _updateRepository = new DishUpdateRepository(_context);
        _feature = new UpdateDishFeature(_updateRepository);
    }

    [Fact]
    public async Task Execute_UpdatesDishSuccessfully()
    {
        // Arrange
        var dishes = await _dishRepository.GetAllWithIngredients();
        var dishToUpdate = dishes.First();
        
        var command = new UpdateDishCommand(
            DishId: dishToUpdate.Id,
            Name: "Updated Dish Name",
            ActiveMinutes: 30,
            TotalMinutes: 60,
            KidRating: 5,
            FamilyRating: 4,
            IsPescetarian: true,
            HasOptionalMeatVariant: true,
            Ingredients: [
                new UpdateDishIngredientItem("Updated Ingredient", "produce", 500, "g", false)
            ]
        );

        // Act
        var result = await _feature.Execute(command);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dishToUpdate.Id, result.Id);

        // Verify changes persisted
        var updated = await _dishRepository.GetByIdWithIngredients(dishToUpdate.Id);
        Assert.NotNull(updated);
        Assert.Equal("Updated Dish Name", updated.Name);
        Assert.Equal(30, updated.ActiveMinutes);
        Assert.Equal(60, updated.TotalMinutes);
        Assert.Equal(5, updated.KidRating);
        Assert.Equal(4, updated.FamilyRating);
        Assert.True(updated.IsPescetarian);
        Assert.True(updated.HasOptionalMeatVariant);
        Assert.Single(updated.Ingredients);
        Assert.Equal("Updated Ingredient", updated.Ingredients[0].Ingredient.Name);
    }

    [Fact]
    public async Task Execute_ReturnsNullForNonExistentDish()
    {
        // Arrange
        var command = new UpdateDishCommand(
            DishId: Guid.NewGuid(),
            Name: "Test",
            ActiveMinutes: 10,
            TotalMinutes: 20,
            KidRating: 3,
            FamilyRating: 3,
            IsPescetarian: false,
            HasOptionalMeatVariant: false,
            Ingredients: [
                new UpdateDishIngredientItem("Test", "test", 100, "g", false)
            ]
        );

        // Act
        var result = await _feature.Execute(command);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Execute_ReplacesAllIngredients()
    {
        // Arrange
        var dishes = await _dishRepository.GetAllWithIngredients();
        var dish = dishes.First(d => d.Name == "Laksepasta");
        var originalIngredientCount = dish.Ingredients.Count;
        Assert.Equal(4, originalIngredientCount);
        
        var command = new UpdateDishCommand(
            DishId: dish.Id,
            Name: dish.Name,
            ActiveMinutes: dish.ActiveMinutes,
            TotalMinutes: dish.TotalMinutes,
            KidRating: dish.KidRating,
            FamilyRating: dish.FamilyRating,
            IsPescetarian: dish.IsPescetarian,
            HasOptionalMeatVariant: dish.HasOptionalMeatVariant,
            Ingredients: [
                new UpdateDishIngredientItem("New Ingredient 1", "produce", 100, "g", false),
                new UpdateDishIngredientItem("New Ingredient 2", "dairy", 200, "ml", false)
            ]
        );

        // Act
        var result = await _feature.Execute(command);

        // Assert
        Assert.NotNull(result);
        
        var updated = await _dishRepository.GetByIdWithIngredients(dish.Id);
        Assert.NotNull(updated);
        Assert.Equal(2, updated.Ingredients.Count);
        Assert.Contains(updated.Ingredients, i => i.Ingredient.Name == "New Ingredient 1");
        Assert.Contains(updated.Ingredients, i => i.Ingredient.Name == "New Ingredient 2");
    }

    [Fact]
    public async Task Execute_ReusesExistingIngredients()
    {
        // Arrange
        var dishes = await _dishRepository.GetAllWithIngredients();
        var dish = dishes.First();
        
        var existingIngredient = dish.Ingredients.First();
        var existingIngredientId = existingIngredient.Ingredient.Id;
        
        var command = new UpdateDishCommand(
            DishId: dish.Id,
            Name: dish.Name,
            ActiveMinutes: dish.ActiveMinutes,
            TotalMinutes: dish.TotalMinutes,
            KidRating: dish.KidRating,
            FamilyRating: dish.FamilyRating,
            IsPescetarian: dish.IsPescetarian,
            HasOptionalMeatVariant: dish.HasOptionalMeatVariant,
            Ingredients: [
                new UpdateDishIngredientItem(
                    existingIngredient.Ingredient.Name,
                    existingIngredient.Ingredient.Category,
                    existingIngredient.Amount,
                    existingIngredient.Unit,
                    existingIngredient.Optional
                )
            ]
        );

        // Act
        await _feature.Execute(command);

        // Assert
        var ingredientCount = await _context.Ingredients.CountAsync();
        Assert.Equal(6, ingredientCount); // Should not create a new ingredient
        
        var updated = await _dishRepository.GetByIdWithIngredients(dish.Id);
        Assert.NotNull(updated);
        Assert.Single(updated.Ingredients);
        Assert.Equal(existingIngredientId, updated.Ingredients[0].Ingredient.Id);
    }

    [Fact]
    public async Task Execute_ThrowsOnInvalidName()
    {
        // Arrange
        var dishes = await _dishRepository.GetAllWithIngredients();
        var dish = dishes.First();
        
        var command = new UpdateDishCommand(
            DishId: dish.Id,
            Name: "",
            ActiveMinutes: 10,
            TotalMinutes: 20,
            KidRating: 3,
            FamilyRating: 3,
            IsPescetarian: false,
            HasOptionalMeatVariant: false,
            Ingredients: [
                new UpdateDishIngredientItem("Test", "test", 100, "g", false)
            ]
        );

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _feature.Execute(command));
    }

    [Fact]
    public async Task Execute_ThrowsOnInvalidTimeValues()
    {
        // Arrange
        var dishes = await _dishRepository.GetAllWithIngredients();
        var dish = dishes.First();
        
        var command = new UpdateDishCommand(
            DishId: dish.Id,
            Name: "Valid Name",
            ActiveMinutes: 100,
            TotalMinutes: 50, // Less than active minutes
            KidRating: 3,
            FamilyRating: 3,
            IsPescetarian: false,
            HasOptionalMeatVariant: false,
            Ingredients: [
                new UpdateDishIngredientItem("Test", "test", 100, "g", false)
            ]
        );

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _feature.Execute(command));
    }

    [Fact]
    public async Task Execute_ThrowsOnInvalidRatings()
    {
        // Arrange
        var dishes = await _dishRepository.GetAllWithIngredients();
        var dish = dishes.First();
        
        var command = new UpdateDishCommand(
            DishId: dish.Id,
            Name: "Valid Name",
            ActiveMinutes: 10,
            TotalMinutes: 20,
            KidRating: 6, // Out of range
            FamilyRating: 3,
            IsPescetarian: false,
            HasOptionalMeatVariant: false,
            Ingredients: [
                new UpdateDishIngredientItem("Test", "test", 100, "g", false)
            ]
        );

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _feature.Execute(command));
    }

    [Fact]
    public async Task Execute_ThrowsOnNoIngredients()
    {
        // Arrange
        var dishes = await _dishRepository.GetAllWithIngredients();
        var dish = dishes.First();
        
        var command = new UpdateDishCommand(
            DishId: dish.Id,
            Name: "Valid Name",
            ActiveMinutes: 10,
            TotalMinutes: 20,
            KidRating: 3,
            FamilyRating: 3,
            IsPescetarian: false,
            HasOptionalMeatVariant: false,
            Ingredients: []
        );

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _feature.Execute(command));
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }
}
