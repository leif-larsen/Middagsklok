using Middagsklok.Api.Domain.Ingredient;
using Middagsklok.Api.Features.Recipes.ScaledIngredients;
using TUnit.Assertions;
using TUnit.Core;

namespace Middagsklok.Tests.Features.Recipes.ScaledIngredients;

public sealed class AmountFormatterTests
{
    // Verifies volumes step from ml to dl to l with a decimal comma and no floating point noise.
    [Test]
    [Arguments(7.5, Unit.Ml, "7,5", "ml")]
    [Arguments(37.5, Unit.Ml, "38", "ml")]
    [Arguments(99.6, Unit.Ml, "1", "dl")]
    [Arguments(113.99999, Unit.Ml, "1,1", "dl")]
    [Arguments(675, Unit.Ml, "6,8", "dl")]
    [Arguments(1125, Unit.Ml, "1,13", "l")]
    [Arguments(0.9, Unit.L, "9", "dl")]
    [Arguments(1.5, Unit.L, "1,5", "l")]
    public async Task FormatsVolumes(double amount, Unit unit, string expectedAmount, string expectedUnit)
    {
        var display = AmountFormatter.Format(amount, unit);

        await Assert.That(display.Amount).IsEqualTo(expectedAmount);
        await Assert.That(display.Unit).IsEqualTo(expectedUnit);
    }

    // Verifies weights stay in grams below a kilo and switch to kg above.
    [Test]
    [Arguments(2.5, Unit.G, "2,5", "g")]
    [Arguments(112.5, Unit.G, "113", "g")]
    [Arguments(600, Unit.G, "600", "g")]
    [Arguments(999.6, Unit.G, "1", "kg")]
    [Arguments(1234, Unit.G, "1,23", "kg")]
    [Arguments(0.75, Unit.Kg, "750", "g")]
    public async Task FormatsWeights(double amount, Unit unit, string expectedAmount, string expectedUnit)
    {
        var display = AmountFormatter.Format(amount, unit);

        await Assert.That(display.Amount).IsEqualTo(expectedAmount);
        await Assert.That(display.Unit).IsEqualTo(expectedUnit);
    }

    // Verifies countable units round to the nearest quarter and never hide a positive amount.
    [Test]
    [Arguments(1.5, Unit.Pcs, "1½", "stk")]
    [Arguments(0.5, Unit.Pcs, "½", "stk")]
    [Arguments(0.75, Unit.Pcs, "¾", "stk")]
    [Arguments(2.2, Unit.Pcs, "2¼", "stk")]
    [Arguments(3, Unit.Pcs, "3", "stk")]
    [Arguments(0.05, Unit.Pcs, "¼", "stk")]
    [Arguments(0.75, Unit.Pack, "¾", "pk")]
    public async Task FormatsCountableUnitsInQuarters(double amount, Unit unit, string expectedAmount, string expectedUnit)
    {
        var display = AmountFormatter.Format(amount, unit);

        await Assert.That(display.Amount).IsEqualTo(expectedAmount);
        await Assert.That(display.Unit).IsEqualTo(expectedUnit);
    }
}
