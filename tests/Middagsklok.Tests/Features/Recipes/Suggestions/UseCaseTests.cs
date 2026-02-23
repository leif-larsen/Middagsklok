using Microsoft.EntityFrameworkCore;
using Middagsklok.Api.Database;
using Middagsklok.Api.Domain.Dish;
using Middagsklok.Api.Features.Recipes.Suggestions;
using TUnit.Assertions;
using TUnit.Core;

namespace Middagsklok.Tests.Features.Recipes.Suggestions;

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

    // Verifies that valid requests return mapped suggestions from the provider client.
    [Test]
    public async Task ReturnsSuggestionsFromProvider()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);
        context.Dishes.Add(new Dish(
            "Veggie Pasta",
            DishType.Pasta,
            10,
            15,
            4,
            "Boil pasta. Toss with vegetables.",
            false,
            true,
            false,
            Array.Empty<DishIngredient>(),
            ["QuickWeeknight"]));
        await context.SaveChangesAsync(CancellationToken.None);

        var selector = new TestSelector(new TestClient());
        var useCase = new UseCase(context, selector);

        var result = await useCase.Execute(new Request("quick vegetarian dinner", 2), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(SuggestionsOutcome.Success);
        await Assert.That(result.Response).IsNotNull();
        await Assert.That(result.Response!.Suggestions.Count).IsEqualTo(2);
        await Assert.That(result.Response.Suggestions[0].Title).IsEqualTo("Green Bowl");
    }

    // Verifies that unavailable provider selection maps to unavailable outcome.
    [Test]
    public async Task ReturnsUnavailableWhenProviderSelectionFails()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var context = CreateContext(databaseName);

        var selector = new TestSelector("Provider unavailable");
        var useCase = new UseCase(context, selector);

        var result = await useCase.Execute(new Request("healthy dinner", 3), CancellationToken.None);

        await Assert.That(result.Outcome).IsEqualTo(SuggestionsOutcome.Unavailable);
        await Assert.That(result.Errors.Any(error => error.Field == "provider")).IsTrue();
    }

    private sealed class TestSelector : IRecipeSuggestionClientSelector
    {
        private readonly IRecipeSuggestionClient? _client;
        private readonly string? _error;

        public TestSelector(IRecipeSuggestionClient client)
        {
            _client = client;
        }

        public TestSelector(string error)
        {
            _error = error;
        }

        // Returns a deterministic provider selection for tests.
        public RecipeSuggestionClientSelection Select()
        {
            if (_client is not null)
            {
                return RecipeSuggestionClientSelection.Success(_client);
            }

            return RecipeSuggestionClientSelection.Failure(_error ?? "Provider unavailable");
        }
    }

    private sealed class TestClient : IRecipeSuggestionClient
    {
        public string ProviderName => "Test";

        // Returns deterministic suggestions for assertions.
        public Task<RecipeSuggestionGenerationResult> Generate(
            RecipeSuggestionGenerationRequest request,
            CancellationToken cancellationToken)
        {
            var suggestions = new[]
            {
                new GeneratedRecipeSuggestion("ai-1", "Green Bowl", "A quick green bowl.", "Fast and healthy", 25),
                new GeneratedRecipeSuggestion("ai-2", "Tofu Stir Fry", "A high-protein stir fry.", "Uses pantry staples", 20)
            };

            var result = RecipeSuggestionGenerationResult.Success(suggestions);

            return Task.FromResult(result);
        }
    }
}
