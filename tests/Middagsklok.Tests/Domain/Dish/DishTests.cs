using Middagsklok.Api.Domain.Dish;
using TUnit.Assertions;
using TUnit.Core;

namespace Middagsklok.Tests.Domain.Dish;

public sealed class DishTests
{
    // Verifies that unknown vibe tags are silently dropped when constructing a Dish.
    [Test]
    public async Task DropsUnknownVibeTagsOnConstruction()
    {
        var dish = new Api.Domain.Dish.Dish(
            "Test Dish",
            DishType.Pasta,
            10,
            20,
            4,
            null,
            false,
            false,
            false,
            [],
            ["ComfortFood", "unknown-tag", "comfort food"]);

        await Assert.That(dish.VibeTags.Count).IsEqualTo(1);
        await Assert.That(dish.VibeTags).Contains("ComfortFood");
    }

    // Verifies that unknown vibe tags are silently dropped when updating a Dish.
    [Test]
    public async Task DropsUnknownVibeTagsOnUpdate()
    {
        var dish = new Api.Domain.Dish.Dish(
            "Test Dish",
            DishType.Pasta,
            10,
            20,
            4,
            null,
            false,
            false,
            false,
            [],
            null);

        dish.Update(
            "Test Dish",
            DishType.Pasta,
            10,
            20,
            4,
            null,
            false,
            false,
            false,
            [],
            ["QuickWeeknight", "free-text-tag"]);

        await Assert.That(dish.VibeTags.Count).IsEqualTo(1);
        await Assert.That(dish.VibeTags).Contains("QuickWeeknight");
    }
}
