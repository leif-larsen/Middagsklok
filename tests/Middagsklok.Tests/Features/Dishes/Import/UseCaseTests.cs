using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.Dish;
using Middagsklok.Api.Domain.Ingredient;
using Middagsklok.Api.Features.Dishes.Import;
using TUnit.Assertions;
using TUnit.Core;

namespace Middagsklok.Tests.Features.Dishes.Import;

public interface IRequestFactory
{
    // Creates a request payload for tests.
    Request Create();
}

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

    // Creates a dish input with a single ingredient.
    private static DishInput CreateDish(string name, string ingredientName)
    {
        var ingredients = new[]
        {
            new IngredientInput(ingredientName, "produce", 1, "stk")
        };

        var dish = new DishInput(name, 10, 20, ingredients);
        return dish;
    }

    // Creates a request containing multiple dishes.
    private static Request CreateRequest(params DishInput[] dishes) => new(dishes);

    // Verifies that dishes are imported and shared ingredients are reused.
    [Test]
    public async Task ImportsDishesAndReusesIngredients()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);
        var useCase = new UseCase(context);

        var request = CreateRequest(
            new DishInput(
                "First Dish",
                10,
                25,
                new[]
                {
                    new IngredientInput("Salt", "produce", 1, "stk"),
                    new IngredientInput("Chicken", "meat", 200, "g")
                }),
            new DishInput(
                "Second Dish",
                5,
                15,
                new[]
                {
                    new IngredientInput("Salt", "produce", 2, "stk")
                }));

        var requestFactory = A.Fake<IRequestFactory>();
        A.CallTo(() => requestFactory.Create()).Returns(request);

        var response = await useCase.Execute(requestFactory.Create(), CancellationToken.None);

        await Assert.That(response.Attempted).IsEqualTo(2);
        await Assert.That(response.Imported).IsEqualTo(2);
        await Assert.That(response.Skipped).IsEqualTo(0);
        await Assert.That(response.Failed).IsEqualTo(0);

        var dishesCount = await context.Dishes.CountAsync(CancellationToken.None);
        var ingredientsCount = await context.Ingredients.CountAsync(CancellationToken.None);

        await Assert.That(dishesCount).IsEqualTo(2);
        await Assert.That(ingredientsCount).IsEqualTo(2);
    }

    // Verifies that existing dish names are skipped during import.
    [Test]
    public async Task SkipsDuplicateDish()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);

        var ingredient = new Ingredient("Salt", IngredientCategory.Produce, Unit.Pcs);
        context.Ingredients.Add(ingredient);
        var existingDish = new Dish(
            "Duplicate Dish",
            DishType.None,
            5,
            10,
            4,
            null,
            false,
            false,
            false,
            new List<DishIngredient>
            {
                new(ingredient.Id, 1, Unit.Pcs, null, 1)
            });

        context.Dishes.Add(existingDish);
        await context.SaveChangesAsync(CancellationToken.None);

        var useCase = new UseCase(context);
        var request = CreateRequest(CreateDish("Duplicate Dish", "Salt"));

        var response = await useCase.Execute(request, CancellationToken.None);

        await Assert.That(response.Attempted).IsEqualTo(1);
        await Assert.That(response.Imported).IsEqualTo(0);
        await Assert.That(response.Skipped).IsEqualTo(1);
        await Assert.That(response.Failed).IsEqualTo(0);

        var dishesCount = await context.Dishes.CountAsync(CancellationToken.None);

        await Assert.That(dishesCount).IsEqualTo(1);
    }

    // Verifies that invalid dishes report failures.
    [Test]
    public async Task ReportsFailuresForInvalidDish()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);
        var useCase = new UseCase(context);

        var request = CreateRequest(new DishInput(string.Empty, 0, 0, Array.Empty<IngredientInput>()));
        var response = await useCase.Execute(request, CancellationToken.None);

        await Assert.That(response.Attempted).IsEqualTo(1);
        await Assert.That(response.Failed).IsEqualTo(1);
        await Assert.That(response.Failures.Count).IsEqualTo(2);

        var hasNameFailure = response.Failures.Any(f => f.Reason.Contains("Dish name is required.", StringComparison.Ordinal));

        await Assert.That(hasNameFailure).IsTrue();
    }

    // Verifies that duplicate ingredient names within a dish are deduplicated.
    [Test]
    public async Task DeduplicatesIngredientsWithinDish()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);
        var useCase = new UseCase(context);

        var request = CreateRequest(new DishInput(
            "Ingredient Dedup Dish",
            5,
            10,
            new[]
            {
                new IngredientInput("Salt", "produce", 1, "stk"),
                new IngredientInput("salt", "produce", 2, "stk")
            }));

        var response = await useCase.Execute(request, CancellationToken.None);

        await Assert.That(response.Imported).IsEqualTo(1);
        await Assert.That(response.Failed).IsEqualTo(0);

        var dish = await context.Dishes.SingleAsync(CancellationToken.None);

        await Assert.That(dish.Ingredients.Count).IsEqualTo(1);
    }

    // Verifies that unknown units fall back to category defaults.
    [Test]
    public async Task MapsUnknownUnitToDefault()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);
        var useCase = new UseCase(context);

        var request = CreateRequest(new DishInput(
            "Unit Default Dish",
            5,
            10,
            new[]
            {
                new IngredientInput("Olive Oil", "oil", 10, "unknown")
            }));

        var response = await useCase.Execute(request, CancellationToken.None);

        await Assert.That(response.Imported).IsEqualTo(1);
        await Assert.That(response.Failed).IsEqualTo(0);

        var dish = await context.Dishes.SingleAsync(CancellationToken.None);
        var ingredient = dish.Ingredients.Single();

        await Assert.That(ingredient.Unit).IsEqualTo(Unit.Ml);
    }

    // Verifies that unknown categories map to Other.
    [Test]
    public async Task MapsUnknownCategoryToOther()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);
        var useCase = new UseCase(context);

        var request = CreateRequest(new DishInput(
            "Category Default Dish",
            5,
            10,
            new[]
            {
                new IngredientInput("Mystery Ingredient", "mystery", 1, "stk")
            }));

        var response = await useCase.Execute(request, CancellationToken.None);

        await Assert.That(response.Imported).IsEqualTo(1);
        await Assert.That(response.Failed).IsEqualTo(0);

        var ingredient = await context.Ingredients.SingleAsync(CancellationToken.None);

        await Assert.That(ingredient.Category).IsEqualTo(IngredientCategory.Other);
    }

    // Verifies that invalid ingredient amounts fail the dish import.
    [Test]
    public async Task ReportsFailureForInvalidIngredientAmount()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);
        var useCase = new UseCase(context);

        var request = CreateRequest(new DishInput(
            "Bad Ingredient Dish",
            5,
            10,
            new[]
            {
                new IngredientInput("Sugar", "produce", 0, "g")
            }));

        var response = await useCase.Execute(request, CancellationToken.None);

        await Assert.That(response.Imported).IsEqualTo(0);
        await Assert.That(response.Failed).IsEqualTo(1);

        var hasAmountFailure = response.Failures.Any(f => f.Reason.Contains("Ingredient amount must be > 0.", StringComparison.Ordinal));

        await Assert.That(hasAmountFailure).IsTrue();
    }

    // Verifies that duplicate dish names within the request are skipped.
    [Test]
    public async Task SkipsDuplicateDishNamesInRequest()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);
        var useCase = new UseCase(context);

        var request = CreateRequest(
            CreateDish("Duplicate In Request", "Salt"),
            CreateDish("duplicate in request", "Pepper"));

        var response = await useCase.Execute(request, CancellationToken.None);

        await Assert.That(response.Attempted).IsEqualTo(2);
        await Assert.That(response.Imported).IsEqualTo(1);
        await Assert.That(response.Skipped).IsEqualTo(1);
        await Assert.That(response.Failed).IsEqualTo(0);
    }
}
