using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.Dish;
using Middagsklok.Api.Domain.Ingredient;
using TUnit.Assertions;
using TUnit.Core;
using DeleteUseCase = Middagsklok.Api.Features.Ingredients.OdaMappings.Delete.UseCase;
using DeleteOutcome = Middagsklok.Api.Features.Ingredients.OdaMappings.Delete.DeleteOutcome;
using DishEntity = Middagsklok.Api.Domain.Dish.Dish;
using IngredientEntity = Middagsklok.Api.Domain.Ingredient.Ingredient;
using OverviewUseCase = Middagsklok.Api.Features.Ingredients.OdaMappings.Overview.UseCase;
using UpsertOutcome = Middagsklok.Api.Features.Ingredients.OdaMappings.Upsert.UpsertOutcome;
using UpsertRequest = Middagsklok.Api.Features.Ingredients.OdaMappings.Upsert.Request;
using UpsertUseCase = Middagsklok.Api.Features.Ingredients.OdaMappings.Upsert.UseCase;

namespace Middagsklok.Tests.Features.Ingredients.OdaMappings;

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

    // Seeds one ingredient and returns it.
    private static async Task<IngredientEntity> SeedIngredient(AppDbContext context, string name = "Kyllingfilet")
    {
        var ingredient = new IngredientEntity(name, IngredientCategory.Poultry, Unit.G);
        context.Ingredients.Add(ingredient);
        await context.SaveChangesAsync(CancellationToken.None);

        return ingredient;
    }

    // Verifies that a product mapping is stored and read back with the ingredient.
    [Test]
    public async Task StoresProductMappingOnIngredient()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);
        var ingredient = await SeedIngredient(context);

        var useCase = new UpsertUseCase(context);
        var request = new UpsertRequest(66899, "Ytterøy Kyllingfilet", 400, "G");
        var result = await useCase.Execute(ingredient.Id.ToString("D"), request, CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(UpsertOutcome.Success);
        await Assert.That(result.Mapping!.ProductId).IsEqualTo(66899);
        await Assert.That(result.Mapping.Availability).IsEqualTo("Available");
        await Assert.That(result.Mapping.Confirmed).IsTrue();

        await using var verify = CreateContext(databaseName);
        var stored = await verify.Ingredients.SingleAsync(i => i.Id == ingredient.Id);

        await Assert.That(stored.OdaMapping).IsNotNull();
        await Assert.That(stored.OdaMapping!.PackQuantity).IsEqualTo(400);
        await Assert.That(stored.OdaMapping.PackUnit).IsEqualTo(Unit.G);
    }

    // Verifies that a second upsert replaces the first mapping instead of adding to it.
    [Test]
    public async Task ReplacesExistingMapping()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);
        var ingredient = await SeedIngredient(context);
        var useCase = new UpsertUseCase(context);
        var id = ingredient.Id.ToString("D");

        await useCase.Execute(id, new UpsertRequest(1, "First", 100, "G"), CancellationToken.None);
        var result = await useCase.Execute(id, new UpsertRequest(2, "Second", 200, "G", false), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(UpsertOutcome.Success);
        await Assert.That(result.Mapping!.ProductId).IsEqualTo(2);
        await Assert.That(result.Mapping.Confirmed).IsFalse();

        await using var verify = CreateContext(databaseName);
        var stored = await verify.Ingredients.SingleAsync(i => i.Id == ingredient.Id);

        await Assert.That(stored.OdaMapping!.OdaProductId).IsEqualTo(2);
    }

    // Verifies that an ingredient can be recorded as not stocked at Oda without product data.
    [Test]
    public async Task StoresNotAvailableWithoutProductData()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);
        var ingredient = await SeedIngredient(context, "Sushi");

        var useCase = new UpsertUseCase(context);
        var request = new UpsertRequest(null, null, null, null, NotAvailable: true);
        var result = await useCase.Execute(ingredient.Id.ToString("D"), request, CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(UpsertOutcome.Success);
        await Assert.That(result.Mapping!.Availability).IsEqualTo("NotAvailable");
        await Assert.That(result.Mapping.ProductId).IsNull();
    }

    // Verifies that product data and the not-available flag cannot be combined.
    [Test]
    public async Task RejectsProductCombinedWithNotAvailable()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);
        var ingredient = await SeedIngredient(context);

        var useCase = new UpsertUseCase(context);
        var request = new UpsertRequest(1, "A", 1, "Pcs", NotAvailable: true);
        var result = await useCase.Execute(ingredient.Id.ToString("D"), request, CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(UpsertOutcome.Invalid);
    }

    // Verifies that incomplete product data is rejected with field-level errors.
    [Test]
    public async Task RejectsIncompleteProductData()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);
        var ingredient = await SeedIngredient(context);

        var useCase = new UpsertUseCase(context);
        var request = new UpsertRequest(0, " ", 0, "Box");
        var result = await useCase.Execute(ingredient.Id.ToString("D"), request, CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(UpsertOutcome.Invalid);
        await Assert.That(result.Errors.Count).IsEqualTo(4);
    }

    // Verifies that mapping an unknown ingredient reports not found.
    [Test]
    public async Task ReportsNotFoundForUnknownIngredient()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);

        var useCase = new UpsertUseCase(context);
        var request = new UpsertRequest(1, "A", 1, "Pcs");
        var result = await useCase.Execute(Guid.NewGuid().ToString("D"), request, CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(UpsertOutcome.NotFound);
    }

    // Verifies that clearing a mapping returns the ingredient to the unmapped state.
    [Test]
    public async Task ClearsMapping()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);
        var ingredient = await SeedIngredient(context);
        var id = ingredient.Id.ToString("D");

        await new UpsertUseCase(context).Execute(id, new UpsertRequest(1, "A", 1, "Pcs"), CancellationToken.None);
        var result = await new DeleteUseCase(context).Execute(id, CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(DeleteOutcome.Success);

        await using var verify = CreateContext(databaseName);
        var stored = await verify.Ingredients.SingleAsync(i => i.Id == ingredient.Id);

        await Assert.That(stored.OdaMapping).IsNull();
    }

    // Verifies that clearing an ingredient without a mapping reports not found.
    [Test]
    public async Task ReportsNotFoundWhenClearingUnmappedIngredient()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);
        var ingredient = await SeedIngredient(context);

        var result = await new DeleteUseCase(context).Execute(ingredient.Id.ToString("D"), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(DeleteOutcome.NotFound);
    }

    // Verifies that the overview splits mapped from unmapped and orders unmapped by usage.
    [Test]
    public async Task OverviewSplitsMappedFromUnmappedOrderedByUsage()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);

        var mapped = new IngredientEntity("Laks", IngredientCategory.Seafood, Unit.G);
        var rare = new IngredientEntity("Dill", IngredientCategory.Produce, Unit.G);
        var common = new IngredientEntity("Gulrot", IngredientCategory.Produce, Unit.Pcs);
        mapped.SetOdaMapping(OdaProductMapping.ForProduct(27247, "Laksefilet", 500, Unit.G, true));
        context.Ingredients.AddRange(mapped, rare, common);

        context.Dishes.AddRange(
            CreateDish("Lapskaus", common.Id),
            CreateDish("Fiskesuppe", common.Id, mapped.Id),
            CreateDish("Laksepasta", rare.Id, mapped.Id));
        await context.SaveChangesAsync(CancellationToken.None);

        var response = await new OverviewUseCase(context).Execute(CancellationToken.None);

        var mappedRow = response.Mappings.Single();
        await Assert.That(mappedRow.IngredientName).IsEqualTo("Laks");
        await Assert.That(mappedRow.UsedIn).IsEqualTo(2);

        var unmappedNames = response.Unmapped.Select(row => row.IngredientName).ToArray();
        await Assert.That(unmappedNames).IsEquivalentTo(new[] { "Gulrot", "Dill" });
        await Assert.That(response.Unmapped.First().UsedIn).IsEqualTo(2);
    }

    // Builds a dish that uses each of the given ingredients once.
    private static DishEntity CreateDish(string name, params Guid[] ingredientIds)
    {
        var ingredients = ingredientIds
            .Select((ingredientId, index) => new DishIngredient(ingredientId, 1, Unit.Pcs, null, index + 1))
            .ToArray();

        var dish = new DishEntity(name, DishType.Other, 10, 10, 4, null, false, false, false, ingredients);

        return dish;
    }
}
