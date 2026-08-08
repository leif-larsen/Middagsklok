using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.Dish;
using Middagsklok.Api.Domain.Ingredient;
using Middagsklok.Api.Features.Dishes.Update;
using TUnit.Assertions;
using TUnit.Core;

namespace Middagsklok.Tests.Features.Dishes.Update;

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

    // Verifies that update persists normalized vibe tags and returns them in the response.
    [Test]
    public async Task PersistsNormalizedVibeTags()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);

        var ingredient = new Ingredient("Salt", IngredientCategory.Other, Unit.Pcs);
        var dish = new Dish(
            "Tag Dish",
            DishType.Pasta,
            10,
            20,
            4,
            null,
            false,
            false,
            false,
            [new DishIngredient(ingredient.Id, 1, Unit.Pcs, null, 1)]);

        context.Ingredients.Add(ingredient);
        context.Dishes.Add(dish);
        await context.SaveChangesAsync(CancellationToken.None);

        var useCase = new UseCase(context);
        var request = new Request(
            "Tag Dish Updated",
            "Pasta",
            10,
            20,
            4,
            null,
            false,
            false,
            false,
            ["weekendtreat", "WeekendTreat", "FamilyFriendly"],
            [new IngredientInput(ingredient.Id.ToString("D"), null, 1)]);

        var result = await useCase.Execute(dish.Id.ToString("D"), request, CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(UpdateOutcome.Success);
        await Assert.That(result.Dish).IsNotNull();

        var responseTags = result.Dish!.VibeTags;
        await Assert.That(responseTags.Count).IsEqualTo(2);
        await Assert.That(responseTags.Contains("WeekendTreat")).IsTrue();
        await Assert.That(responseTags.Contains("FamilyFriendly")).IsTrue();

        var persistedDish = await context.Dishes.AsNoTracking().SingleAsync(CancellationToken.None);
        await Assert.That(persistedDish.VibeTags.Count).IsEqualTo(2);
        await Assert.That(persistedDish.VibeTags.Contains("WeekendTreat")).IsTrue();
        await Assert.That(persistedDish.VibeTags.Contains("FamilyFriendly")).IsTrue();
    }

    // Verifies that a client which omits unit and scaling does not reset stored portion data.
    [Test]
    public async Task PreservesStoredScalingWhenRequestOmitsIt()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);

        var ingredient = new Ingredient("Kjøttdeig", IngredientCategory.Meat, Unit.Pcs);
        var dish = new Dish(
            "Taco",
            DishType.WrapTaco,
            10,
            20,
            4,
            null,
            false,
            false,
            false,
            [new DishIngredient(ingredient.Id, 75, Unit.G, null, 1, IngredientScaling.PerServing)]);

        context.Ingredients.Add(ingredient);
        context.Dishes.Add(dish);
        await context.SaveChangesAsync(CancellationToken.None);

        var useCase = new UseCase(context);

        // Mirrors what the pre-scaling frontend sends: id and amount only.
        var request = new Request(
            "Taco",
            "WrapTaco",
            10,
            20,
            4,
            null,
            false,
            false,
            false,
            null,
            [new IngredientInput(ingredient.Id.ToString("D"), null, 80)]);

        var result = await useCase.Execute(dish.Id.ToString("D"), request, CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(UpdateOutcome.Success);

        var persisted = await context.Dishes
            .AsNoTracking()
            .Include(existing => existing.Ingredients)
            .SingleAsync(CancellationToken.None);
        var persistedIngredient = persisted.Ingredients.Single();

        await Assert.That(persistedIngredient.Quantity).IsEqualTo(80d);
        await Assert.That(persistedIngredient.Unit).IsEqualTo(Unit.G);
        await Assert.That(persistedIngredient.Scaling).IsEqualTo(IngredientScaling.PerServing);
    }

    // Verifies that an explicit scaling change still overwrites the stored value.
    [Test]
    public async Task AppliesExplicitScalingWhenProvided()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);

        var ingredient = new Ingredient("Kyllingfilet", IngredientCategory.Poultry, Unit.Pcs);
        var dish = new Dish(
            "Salat",
            DishType.Salad,
            10,
            20,
            4,
            null,
            false,
            false,
            false,
            [new DishIngredient(ingredient.Id, 1, Unit.Pcs, null, 1)]);

        context.Ingredients.Add(ingredient);
        context.Dishes.Add(dish);
        await context.SaveChangesAsync(CancellationToken.None);

        var useCase = new UseCase(context);
        var request = new Request(
            "Salat",
            "Salad",
            10,
            20,
            4,
            null,
            false,
            false,
            false,
            null,
            [new IngredientInput(ingredient.Id.ToString("D"), null, 150, "G", "PerPerson", 1)]);

        var result = await useCase.Execute(dish.Id.ToString("D"), request, CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(UpdateOutcome.Success);

        var persisted = await context.Dishes
            .AsNoTracking()
            .Include(existing => existing.Ingredients)
            .SingleAsync(CancellationToken.None);
        var persistedIngredient = persisted.Ingredients.Single();

        await Assert.That(persistedIngredient.Unit).IsEqualTo(Unit.G);
        await Assert.That(persistedIngredient.Scaling).IsEqualTo(IngredientScaling.PerPerson);
        await Assert.That(persistedIngredient.PersonCount).IsEqualTo(1);
    }
}
