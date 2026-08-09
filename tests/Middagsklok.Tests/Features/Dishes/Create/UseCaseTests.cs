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

    // Verifies that a bare name denoting an existing ingredient is rejected instead of creating a duplicate.
    [Test]
    public async Task RejectsIngredientNameThatDuplicatesAnExistingIngredient()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);

        context.Ingredients.Add(new Ingredient("Potet", IngredientCategory.Produce, Unit.G));
        await context.SaveChangesAsync(CancellationToken.None);

        var useCase = new UseCase(context);
        var request = new Request(
            "Duplicate Dish",
            "Pasta",
            10,
            20,
            4,
            null,
            false,
            false,
            false,
            null,
            [new IngredientInput(null, "poteter", 500)]);

        var result = await useCase.Execute(request, CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(CreateOutcome.Invalid);
        await Assert.That(result.Errors.Count).IsEqualTo(1);
        await Assert.That(result.Errors[0].Message).Contains("Potet");

        var ingredientCount = await context.Ingredients.CountAsync(CancellationToken.None);
        await Assert.That(ingredientCount).IsEqualTo(1);

        var dishCount = await context.Dishes.CountAsync(CancellationToken.None);
        await Assert.That(dishCount).IsEqualTo(0);
    }

    // Verifies that an unrelated bare name still creates an ingredient.
    [Test]
    public async Task CreatesIngredientForUnrelatedName()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);

        context.Ingredients.Add(new Ingredient("Potet", IngredientCategory.Produce, Unit.G));
        await context.SaveChangesAsync(CancellationToken.None);

        var useCase = new UseCase(context);
        var request = new Request(
            "Fresh Dish",
            "Pasta",
            10,
            20,
            4,
            null,
            false,
            false,
            false,
            null,
            [new IngredientInput(null, "Brokkoli", 300)]);

        var result = await useCase.Execute(request, CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(CreateOutcome.Success);

        var ingredientCount = await context.Ingredients.CountAsync(CancellationToken.None);
        await Assert.That(ingredientCount).IsEqualTo(2);
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
