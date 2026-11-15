using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.Dish;
using Middagsklok.Api.Domain.Ingredient;
using Middagsklok.Api.Domain.Settings;
using Middagsklok.Api.Features.Recipes.ScaledIngredients;
using TUnit.Assertions;
using TUnit.Core;
using DishEntity = Middagsklok.Api.Domain.Dish.Dish;
using IngredientEntity = Middagsklok.Api.Domain.Ingredient.Ingredient;

namespace Middagsklok.Tests.Features.Recipes.ScaledIngredients;

public sealed class UseCaseTests
{
    // Creates an in-memory AppDbContext for test isolation.
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        var context = new AppDbContext(options);

        return context;
    }

    // Seeds a soup with per-serving, per-dish and per-person ingredients, plus optional settings.
    private static async Task<DishEntity> SeedSoup(AppDbContext context, int? householdSize)
    {
        var cream = new IngredientEntity("Matfløte", IngredientCategory.DairyAndEggs, Unit.Ml);
        var cauliflower = new IngredientEntity("Blomkål", IngredientCategory.Produce, Unit.G);
        var garlic = new IngredientEntity("Hvitløksfedd", IngredientCategory.Produce, Unit.Pcs);
        var lemon = new IngredientEntity("Sitron", IngredientCategory.Produce, Unit.Pcs);
        context.Ingredients.AddRange(cream, cauliflower, garlic, lemon);

        var dish = new DishEntity(
            "Blomkålsuppe med smørstekt brød",
            DishType.SoupStew,
            10,
            30,
            4,
            null,
            false,
            true,
            false,
            [
                new DishIngredient(cauliflower.Id, 200, Unit.G, null, 1, IngredientScaling.PerServing),
                new DishIngredient(cream.Id, 38, Unit.Ml, null, 2, IngredientScaling.PerServing),
                new DishIngredient(garlic.Id, 3, Unit.Pcs, null, 3, IngredientScaling.PerDish),
                new DishIngredient(lemon.Id, 0.5, Unit.Pcs, "til servering", 4, IngredientScaling.PerPerson, 1)
            ],
            ["Comfort"]);

        context.Dishes.Add(dish);

        if (householdSize is not null)
        {
            context.PlanningSettings.Add(new PlanningSettings(DayOfWeek.Friday, 2, 14, householdSize.Value));
        }

        await context.SaveChangesAsync(CancellationToken.None);

        return dish;
    }

    // Runs the use case and returns the successful response.
    private static async Task<Response> RunSuccess(AppDbContext context, Guid dishId, int? servings)
    {
        var useCase = new UseCase(context);
        var result = await useCase.Execute(dishId.ToString("D"), servings, CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(FetchOutcome.Success);
        await Assert.That(result.Recipe).IsNotNull();

        return result.Recipe!;
    }

    // Finds the scaled ingredient with the given name.
    private static ScaledIngredient Find(Response response, string name) =>
        response.Ingredients.Single(ingredient => ingredient.Name == name);

    // Verifies that per-serving amounts scale while per-dish and per-person amounts stay fixed.
    [Test]
    public async Task ScalesPerServingAmountsOnly()
    {
        await using var context = CreateContext();
        var dish = await SeedSoup(context, householdSize: 3);

        var response = await RunSuccess(context, dish.Id, servings: 3);

        await Assert.That(response.Servings).IsEqualTo(3);
        await Assert.That(response.Serves).IsEqualTo(4);

        var cream = Find(response, "Matfløte");
        await Assert.That(cream.Amount).IsEqualTo(114d);
        await Assert.That(cream.DisplayAmount).IsEqualTo("1,1");
        await Assert.That(cream.DisplayUnit).IsEqualTo("dl");
        await Assert.That(cream.Scaling).IsEqualTo(nameof(IngredientScaling.PerServing));

        await Assert.That(Find(response, "Blomkål").Amount).IsEqualTo(600d);
        await Assert.That(Find(response, "Hvitløksfedd").Amount).IsEqualTo(3d);

        var lemon = Find(response, "Sitron");
        await Assert.That(lemon.Amount).IsEqualTo(0.5d);
        await Assert.That(lemon.DisplayAmount).IsEqualTo("½");
        await Assert.That(lemon.Note).IsEqualTo("til servering");
    }

    // Verifies that a requested servings count overrides the household size.
    [Test]
    public async Task UsesRequestedServings()
    {
        await using var context = CreateContext();
        var dish = await SeedSoup(context, householdSize: 3);

        var response = await RunSuccess(context, dish.Id, servings: 6);

        await Assert.That(response.Servings).IsEqualTo(6);
        await Assert.That(Find(response, "Matfløte").Amount).IsEqualTo(228d);
        await Assert.That(Find(response, "Hvitløksfedd").Amount).IsEqualTo(3d);
    }

    // Verifies that the configured household size is the default servings count.
    [Test]
    public async Task DefaultsToHouseholdSize()
    {
        await using var context = CreateContext();
        var dish = await SeedSoup(context, householdSize: 5);

        var response = await RunSuccess(context, dish.Id, servings: null);

        await Assert.That(response.Servings).IsEqualTo(5);
        await Assert.That(Find(response, "Blomkål").Amount).IsEqualTo(1000d);
        await Assert.That(Find(response, "Blomkål").DisplayUnit).IsEqualTo("kg");
    }

    // Verifies the fallback household size when no settings have been saved.
    [Test]
    public async Task DefaultsToBuiltInHouseholdSizeWithoutSettings()
    {
        await using var context = CreateContext();
        var dish = await SeedSoup(context, householdSize: null);

        var response = await RunSuccess(context, dish.Id, servings: null);

        await Assert.That(response.Servings).IsEqualTo(PlanningSettings.DefaultHouseholdSize);
    }

    // Verifies that an unknown dish is reported as not found.
    [Test]
    public async Task ReturnsNotFoundForUnknownDish()
    {
        await using var context = CreateContext();

        var useCase = new UseCase(context);
        var result = await useCase.Execute(Guid.NewGuid().ToString("D"), 3, CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(FetchOutcome.NotFound);
    }

    // Verifies that malformed ids and out-of-range servings are rejected.
    [Test]
    [Arguments("not-a-guid", 3)]
    [Arguments("8d5e9a3c-62f1-4f0e-9c55-6b1f5f0c9a11", 0)]
    [Arguments("8d5e9a3c-62f1-4f0e-9c55-6b1f5f0c9a11", 31)]
    public async Task RejectsInvalidInput(string dishId, int servings)
    {
        await using var context = CreateContext();

        var useCase = new UseCase(context);
        var result = await useCase.Execute(dishId, servings, CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(FetchOutcome.Invalid);
        await Assert.That(result.Errors.Count).IsEqualTo(1);
    }
}
