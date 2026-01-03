using Microsoft.EntityFrameworkCore;
using Middagsklok.Database;
using Middagsklok.Database.Repositories;

namespace Middagsklok.Tests.Integration;

public class DishRepositoryTests : IDisposable
{
    private readonly MiddagsklokDbContext _context;
    private readonly DishRepository _repository;

    public DishRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MiddagsklokDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new MiddagsklokDbContext(options);
        _context.Database.OpenConnection();
        
        var bootstrapper = new DbBootstrapper(_context);
        bootstrapper.InitializeAsync().GetAwaiter().GetResult();

        _repository = new DishRepository(_context);
    }

    [Fact]
    public async Task GetAllWithIngredients_ReturnsDishesOrderedByName()
    {
        var dishes = await _repository.GetAllWithIngredients();

        Assert.Equal(2, dishes.Count);
        Assert.Equal("Laksepasta", dishes[0].Name);
        Assert.Equal("Pasta med tomatsaus", dishes[1].Name);
    }

    [Fact]
    public async Task GetAllWithIngredients_ReturnsCorrectIngredientCounts()
    {
        var dishes = await _repository.GetAllWithIngredients();

        var laksepasta = dishes.First(d => d.Name == "Laksepasta");
        var tomatSauce = dishes.First(d => d.Name == "Pasta med tomatsaus");

        Assert.Equal(4, laksepasta.Ingredients.Count);
        Assert.Equal(5, tomatSauce.Ingredients.Count);
    }

    [Fact]
    public async Task GetAllWithIngredients_IncludesOptionalFlag()
    {
        var dishes = await _repository.GetAllWithIngredients();

        var tomatSauce = dishes.First(d => d.Name == "Pasta med tomatsaus");
        var optionalIngredient = tomatSauce.Ingredients.FirstOrDefault(i => i.Optional);

        Assert.NotNull(optionalIngredient);
        Assert.Equal("Parmesan", optionalIngredient.Ingredient.Name);
    }

    [Fact]
    public async Task GetAllWithIngredients_IngredientsOrderedByName()
    {
        var dishes = await _repository.GetAllWithIngredients();

        var tomatSauce = dishes.First(d => d.Name == "Pasta med tomatsaus");
        var ingredientNames = tomatSauce.Ingredients.Select(i => i.Ingredient.Name).ToList();

        Assert.Equal(ingredientNames.OrderBy(n => n).ToList(), ingredientNames);
    }

    [Fact]
    public async Task GetAllWithIngredients_DishFieldsAreMapped()
    {
        var dishes = await _repository.GetAllWithIngredients();

        var laksepasta = dishes.First(d => d.Name == "Laksepasta");

        Assert.Equal(15, laksepasta.ActiveMinutes);
        Assert.Equal(25, laksepasta.TotalMinutes);
        Assert.Equal(3, laksepasta.KidRating);
        Assert.Equal(5, laksepasta.FamilyRating);
        Assert.True(laksepasta.IsPescetarian);
        Assert.False(laksepasta.HasOptionalMeatVariant);
    }

    [Fact]
    public async Task Bootstrapper_SeedsWhenEmpty()
    {
        // Create a fresh in-memory database
        var options = new DbContextOptionsBuilder<MiddagsklokDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        using var context = new MiddagsklokDbContext(options);
        context.Database.OpenConnection();
        
        var bootstrapper = new DbBootstrapper(context);
        await bootstrapper.InitializeAsync();

        var dishCount = await context.Dishes.CountAsync();
        var ingredientCount = await context.Ingredients.CountAsync();

        Assert.Equal(2, dishCount);
        Assert.Equal(6, ingredientCount);
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }
}
