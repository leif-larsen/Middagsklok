using Middagsklok.Api.Features.Recipes.Suggestions;
using TUnit.Assertions;
using TUnit.Core;

namespace Middagsklok.Tests.Features.Recipes.Suggestions;

public sealed class ValidatorTests
{
    // Verifies that an empty prompt is rejected.
    [Test]
    public async Task RejectsMissingPrompt()
    {
        var validator = new Validator();

        var result = validator.Validate(new Request("   ", 3));

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(error => error.Field == "prompt")).IsTrue();
    }

    // Verifies that maxSuggestions must stay in accepted bounds.
    [Test]
    public async Task RejectsOutOfRangeMaxSuggestions()
    {
        var validator = new Validator();

        var result = validator.Validate(new Request("quick dinner", 99));

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(error => error.Field == "maxSuggestions")).IsTrue();
    }

    // Verifies that a valid request defaults maxSuggestions when omitted.
    [Test]
    public async Task AppliesDefaultMaxSuggestions()
    {
        var validator = new Validator();

        var result = validator.Validate(new Request("vegan meal", null));

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.Candidate).IsNotNull();
        await Assert.That(result.Candidate!.MaxSuggestions).IsEqualTo(5);
    }
}
