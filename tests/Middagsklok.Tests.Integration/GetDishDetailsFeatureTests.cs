using Microsoft.EntityFrameworkCore;
using Middagsklok.Database;
using Middagsklok.Database.Repositories;
using Middagsklok.Features.Dishes.GetDishDetails;

namespace Middagsklok.Tests.Integration;

public class GetDishDetailsFeatureTests : IDisposable
{
    private readonly MiddagsklokDbContext _context;
    private readonly DishRepository _repository;
    private readonly GetDishDetailsFeature _feature;

    public GetDishDetailsFeatureTests()
    {
        var options = new DbContextOptionsBuilder<MiddagsklokDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new MiddagsklokDbContext(options);
        _context.Database.OpenConnection();
        
        var bootstrapper = new DbBootstrapper(_context);
        bootstrapper.InitializeAsync().GetAwaiter().GetResult();

        _repository = new DishRepository(_context);
        _feature = new GetDishDetailsFeature(_repository);
    }

    [Fact]
    public async Task Execute_ReturnsExistingDish()
    {
        // Arrange
        var dishes = await _repository.GetAllWithIngredients();
        var existingDish = dishes.First();
        var query = new GetDishDetailsQuery(existingDish.Id);

        // Act
        var result = await _feature.Execute(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingDish.Id, result.Id);
        Assert.Equal(existingDish.Name, result.Name);
    }

    [Fact]
    public async Task Execute_ReturnsNullForNonExistentDish()
    {
        // Arrange
        var query = new GetDishDetailsQuery(Guid.NewGuid());

        // Act
        var result = await _feature.Execute(query);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Execute_IncludesIngredients()
    {
        // Arrange
        var dishes = await _repository.GetAllWithIngredients();
        var dish = dishes.First(d => d.Name == "Laksepasta");
        var query = new GetDishDetailsQuery(dish.Id);

        // Act
        var result = await _feature.Execute(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Ingredients.Count);
    }

    [Fact]
    public async Task Execute_IngredientsOrderedByName()
    {
        // Arrange
        var dishes = await _repository.GetAllWithIngredients();
        var dish = dishes.First(d => d.Name == "Pasta med tomatsaus");
        var query = new GetDishDetailsQuery(dish.Id);

        // Act
        var result = await _feature.Execute(query);

        // Assert
        Assert.NotNull(result);
        var ingredientNames = result.Ingredients.Select(i => i.Ingredient.Name).ToList();
        Assert.Equal(ingredientNames.OrderBy(n => n).ToList(), ingredientNames);
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }
}
