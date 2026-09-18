using TUnit.Assertions;
using TUnit.Core;
using CreateIngredientInput = Middagsklok.Api.Features.Dishes.Create.IngredientInput;
using CreateRequest = Middagsklok.Api.Features.Dishes.Create.Request;
using CreateValidator = Middagsklok.Api.Features.Dishes.Create.Validator;
using UpdateIngredientInput = Middagsklok.Api.Features.Dishes.Update.IngredientInput;
using UpdateRequest = Middagsklok.Api.Features.Dishes.Update.Request;
using UpdateValidator = Middagsklok.Api.Features.Dishes.Update.Validator;

namespace Middagsklok.Tests.Features.Dishes;

// Verifies that create and update agree on how an unknown vibe tag is rejected, so a client
// (including the MCP tool clients) sees the same field name and message from either verb.
public sealed class VibeTagValidationParityTests
{
    [Test]
    public async Task CreateAndUpdateRejectTheSameUnknownVibeTagIdentically()
    {
        var createValidator = new CreateValidator();
        var createRequest = new CreateRequest(
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
            [new CreateIngredientInput(null, "Salt", 1)]);

        var updateValidator = new UpdateValidator();
        var updateRequest = new UpdateRequest(
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
            [new UpdateIngredientInput(null, "Salt", 1)]);

        var createResult = createValidator.Validate(createRequest);
        var updateResult = updateValidator.Validate(Guid.NewGuid().ToString("D"), updateRequest);

        await Assert.That(createResult.IsValid).IsFalse();
        await Assert.That(updateResult.IsValid).IsFalse();

        var createError = createResult.Errors.Single(error => error.Field == "vibeTags[0]");
        var updateError = updateResult.Errors.Single(error => error.Field == "vibeTags[0]");

        await Assert.That(updateError.Field).IsEqualTo(createError.Field);
        await Assert.That(updateError.Message).IsEqualTo(createError.Message);
    }
}
