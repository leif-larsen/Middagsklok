using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.Dish;
using Middagsklok.Api.Domain.Ingredient;
using Middagsklok.Api.Features.Dishes.Overview;
using TUnit.Assertions;
using TUnit.Core;

namespace Middagsklok.Tests.Features.Dishes.Overview;

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

    // Seeds one active and one retired dish.
    private static async Task Seed(AppDbContext context)
    {
        var ingredient = new Ingredient("Potet", IngredientCategory.Produce, Unit.G);

        var active = new Dish(
            "Active Dish",
            DishType.SoupStew,
            10,
            20,
            4,
            null,
            false,
            false,
            false,
            [new DishIngredient(ingredient.Id, 1, Unit.Pcs, null, 1)]);

        var retired = new Dish(
            "Retired Dish",
            DishType.SoupStew,
            10,
            20,
            4,
            null,
            false,
            false,
            false,
            [new DishIngredient(ingredient.Id, 1, Unit.Pcs, null, 1)]);

        retired.Retire();

        context.Ingredients.Add(ingredient);
        context.Dishes.AddRange(active, retired);
        await context.SaveChangesAsync(CancellationToken.None);
    }

    // Verifies that the default excludes retired dishes, which is what a client omitting the
    // includeRetired parameter gets.
    [Test]
    public async Task ExcludesRetiredDishesByDefault()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);
        await Seed(context);

        var useCase = new UseCase(context);
        var response = await useCase.Execute(false, CancellationToken.None);

        await Assert.That(response.Dishes.Count()).IsEqualTo(1);
        await Assert.That(response.Dishes.Single().Name).IsEqualTo("Active Dish");
    }

    // Verifies that retired dishes are returned when explicitly requested.
    [Test]
    public async Task IncludesRetiredDishesWhenRequested()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);
        await Seed(context);

        var useCase = new UseCase(context);
        var response = await useCase.Execute(true, CancellationToken.None);

        await Assert.That(response.Dishes.Count()).IsEqualTo(2);

        var retired = response.Dishes.Single(dish => dish.Name == "Retired Dish");
        await Assert.That(retired.RetiredAt).IsNotNull();
    }
}
