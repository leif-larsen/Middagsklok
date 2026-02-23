using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.Dish;
using Middagsklok.Api.Features.Recipes.Instructions;
using TUnit.Assertions;
using TUnit.Core;

namespace Middagsklok.Tests.Features.Recipes.Instructions;

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

    // Verifies that free-form instructions are mapped into ordered steps.
    [Test]
    public async Task MapsInstructionsToSteps()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);

        context.Dishes.Add(new Dish(
            "Pasta Primavera",
            DishType.Pasta,
            10,
            15,
            4,
            "1. Boil water\n2. Cook pasta\n3. Toss with vegetables",
            false,
            true,
            false,
            Array.Empty<DishIngredient>(),
            ["QuickWeeknight"]));

        context.Dishes.Add(new Dish(
            "Simple Salad",
            DishType.Salad,
            5,
            0,
            2,
            null,
            false,
            true,
            true,
            Array.Empty<DishIngredient>(),
            ["Fresh"]));

        await context.SaveChangesAsync(CancellationToken.None);

        var useCase = new UseCase(context);
        var result = await useCase.Execute(CancellationToken.None);

        await Assert.That(result.Recipes.Count).IsEqualTo(2);
        var pasta = result.Recipes.First(recipe => recipe.DishName == "Pasta Primavera");
        var salad = result.Recipes.First(recipe => recipe.DishName == "Simple Salad");

        await Assert.That(pasta.Steps.Count).IsEqualTo(3);
        await Assert.That(pasta.Steps[0].Description).IsEqualTo("Boil water");
        await Assert.That(salad.Steps[0].Description).IsEqualTo("No instructions provided yet.");
    }
}
