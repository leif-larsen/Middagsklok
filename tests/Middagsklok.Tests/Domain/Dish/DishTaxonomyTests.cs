using Middagsklok.Api.Domain.Dish;
using TUnit.Assertions;
using TUnit.Core;

namespace Middagsklok.Tests.Domain.Dish;

public sealed class DishTaxonomyTests
{
    // Verifies that legacy dishType values map to planner-facing dish types.
    [Test]
    public async Task NormalizesLegacyDishTypeValues()
    {
        var normalized = DishTaxonomy.NormalizeType(DishType.Mexican);

        await Assert.That(normalized).IsEqualTo(DishType.WrapTaco);
    }

    // Verifies that weekend and weekday default weights can differ for the same type.
    [Test]
    public async Task ReturnsWeekendAwareDefaultWeight()
    {
        var weekdayWeight = DishTaxonomy.GetDefaultWeight(DishType.PizzaPie, DayOfWeek.Wednesday);
        var weekendWeight = DishTaxonomy.GetDefaultWeight(DishType.PizzaPie, DayOfWeek.Saturday);

        await Assert.That(weekendWeight).IsGreaterThan(weekdayWeight);
    }

    // Verifies that vibe tags normalize to canonical taxonomy values.
    [Test]
    public async Task NormalizesKnownVibeTag()
    {
        var isKnown = DishTaxonomy.TryNormalizeVibeTag("comfortfood", out var normalizedTag);

        await Assert.That(isKnown).IsTrue();
        await Assert.That(normalizedTag).IsEqualTo("ComfortFood");
    }

    // Verifies that vibe multipliers vary between weekday and weekend.
    [Test]
    public async Task ReturnsWeekendAwareVibeMultiplier()
    {
        var weekdayMultiplier = DishTaxonomy.GetVibeWeightMultiplier("ComfortFood", DayOfWeek.Wednesday);
        var weekendMultiplier = DishTaxonomy.GetVibeWeightMultiplier("ComfortFood", DayOfWeek.Saturday);

        await Assert.That(weekendMultiplier).IsGreaterThan(weekdayMultiplier);
    }
}
