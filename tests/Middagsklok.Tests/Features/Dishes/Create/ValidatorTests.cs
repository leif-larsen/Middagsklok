using Middagsklok.Api.Features.Dishes.Create;
using TUnit.Assertions;
using TUnit.Core;

namespace Middagsklok.Tests.Features.Dishes.Create;

public sealed class ValidatorTests
{
    // Verifies that legacy cuisine values are rejected in create requests.
    [Test]
    public async Task RejectsLegacyCuisineValue()
    {
        var validator = new Validator();
        var request = new Request(
            "Test Dish",
            "Italian",
            10,
            20,
            4,
            null,
            false,
            false,
            false,
            Array.Empty<string>(),
            [new IngredientInput(null, "Salt", 1)]);

        var result = validator.Validate(request);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(error => error.Field == "cuisine")).IsTrue();
    }

    // Verifies that unknown vibe tags are rejected in create requests.
    [Test]
    public async Task RejectsUnknownVibeTag()
    {
        var validator = new Validator();
        var request = new Request(
            "Test Dish",
            "Pasta",
            10,
            20,
            4,
            null,
            false,
            false,
            false,
            ["CozyNight"],
            [new IngredientInput(null, "Salt", 1)]);

        var result = validator.Validate(request);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(error => error.Field == "vibeTags[0]")).IsTrue();
    }
}
