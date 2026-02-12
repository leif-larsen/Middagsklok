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
}
