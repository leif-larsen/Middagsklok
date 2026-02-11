using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.Dish;
using Middagsklok.Api.Domain.Ingredient;
using Middagsklok.Api.Features.Dishes.Create;
using TUnit.Assertions;
using TUnit.Core;

namespace Middagsklok.Tests.Features.Dishes.Create;

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

    // Verifies that create persists normalized vibe tags and returns them in the response.
    [Test]
    public async Task PersistsNormalizedVibeTags()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);

        var ingredient = new Ingredient("Salt", IngredientCategory.Other, Unit.Pcs);
        context.Ingredients.Add(ingredient);
        await context.SaveChangesAsync(CancellationToken.None);

        var useCase = new UseCase(context);
        var request = new Request(
            "Tag Dish",
            "Pasta",
            10,
            20,
            4,
            null,
            false,
            false,
            false,
            ["comfortfood", "ComfortFood", "QuickWeeknight"],
            [new IngredientInput(ingredient.Id.ToString("D"), null, 1)]);

        var result = await useCase.Execute(request, CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(CreateOutcome.Success);
        await Assert.That(result.Dish).IsNotNull();

        var responseTags = result.Dish!.VibeTags;
        await Assert.That(responseTags.Count).IsEqualTo(2);
        await Assert.That(responseTags.Contains("ComfortFood")).IsTrue();
        await Assert.That(responseTags.Contains("QuickWeeknight")).IsTrue();

        var persistedDish = await context.Dishes.AsNoTracking().SingleAsync(CancellationToken.None);
        await Assert.That(persistedDish.VibeTags.Count).IsEqualTo(2);
        await Assert.That(persistedDish.VibeTags.Contains("ComfortFood")).IsTrue();
        await Assert.That(persistedDish.VibeTags.Contains("QuickWeeknight")).IsTrue();
    }
}
