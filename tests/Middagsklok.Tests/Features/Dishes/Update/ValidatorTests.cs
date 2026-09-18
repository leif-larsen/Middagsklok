using Middagsklok.Api.Features.Dishes.Update;
using TUnit.Assertions;
using TUnit.Core;

namespace Middagsklok.Tests.Features.Dishes.Update;

public sealed class ValidatorTests
{
    // Verifies that legacy dishType values are rejected in update requests.
    [Test]
    public async Task RejectsLegacyDishTypeValue()
    {
        var validator = new Validator();
        var request = new Request(
            "Test Dish",
            "MiddleEastern",
            10,
            20,
            4,
            null,
            false,
            false,
            false,
            Array.Empty<string>(),
            [new IngredientInput(null, "Salt", 1)]);

        var result = validator.Validate(Guid.NewGuid().ToString("D"), request);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(error => error.Field == "dishType")).IsTrue();
    }

    // Verifies that unknown vibe tags are rejected in update requests.
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

        var result = validator.Validate(Guid.NewGuid().ToString("D"), request);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(error => error.Field == "vibeTags[0]")).IsTrue();
    }

    // Verifies that a dish round-tripped with a mix of casings of a known vibe tag retains only one, deduplicated entry.
    [Test]
    public async Task RoundTripWithDuplicateVibeTagCasingKeepsOneEntry()
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
            ["comfortfood", "ComfortFood"],
            [new IngredientInput(null, "Salt", 1)]);

        var result = validator.Validate(Guid.NewGuid().ToString("D"), request);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.Candidate!.VibeTags).IsEquivalentTo(["ComfortFood"]);
    }
}
