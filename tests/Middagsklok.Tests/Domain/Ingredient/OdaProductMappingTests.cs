using Middagsklok.Api.Domain.Ingredient;
using TUnit.Assertions;
using TUnit.Core;

namespace Middagsklok.Tests.Domain.Ingredient;

public sealed class OdaProductMappingTests
{
    // Verifies that same-unit amounts round up to whole packages.
    [Test]
    public async Task RoundsUpToWholePackagesForSameUnit()
    {
        var mapping = OdaProductMapping.ForProduct(66899, "Kyllingfilet", 400, Unit.G, true);

        await Assert.That(mapping.SuggestedPackCount(600, Unit.G)).IsEqualTo(2);
        await Assert.That(mapping.SuggestedPackCount(400, Unit.G)).IsEqualTo(1);
        await Assert.That(mapping.SuggestedPackCount(401, Unit.G)).IsEqualTo(2);
    }

    // Verifies that grams convert against a kilogram pack and millilitres against a litre pack.
    [Test]
    public async Task ConvertsWithinWeightAndVolumeDimensions()
    {
        var weight = OdaProductMapping.ForProduct(1, "Mel", 1, Unit.Kg, true);
        var volume = OdaProductMapping.ForProduct(2, "Melk", 1.75, Unit.L, true);

        await Assert.That(weight.SuggestedPackCount(1500, Unit.G)).IsEqualTo(2);
        await Assert.That(volume.SuggestedPackCount(951, Unit.Ml)).IsEqualTo(1);
        await Assert.That(volume.SuggestedPackCount(3.5, Unit.L)).IsEqualTo(2);
    }

    // Verifies that a piece count against a weight-based pack treats each piece as one package.
    [Test]
    public async Task TreatsPiecesAsWholePackagesAgainstWeightPacks()
    {
        var mapping = OdaProductMapping.ForProduct(3, "Hakkede tomater", 400, Unit.G, true);

        await Assert.That(mapping.SuggestedPackCount(1, Unit.Pcs)).IsEqualTo(1);
        await Assert.That(mapping.SuggestedPackCount(2.5, Unit.Pcs)).IsEqualTo(3);
    }

    // Verifies that pieces against a piece-based multipack divide into packages.
    [Test]
    public async Task DividesPiecesAcrossPieceBasedMultipacks()
    {
        var mapping = OdaProductMapping.ForProduct(3487, "Stand'n Stuff 8 stk", 8, Unit.Pcs, true);

        await Assert.That(mapping.SuggestedPackCount(8.01, Unit.Pcs)).IsEqualTo(2);
        await Assert.That(mapping.SuggestedPackCount(8, Unit.Pcs)).IsEqualTo(1);
    }

    // Verifies that pack fractions already express the package count and ignore the pack size.
    [Test]
    public async Task RoundsPackFractionsUpRegardlessOfPackSize()
    {
        var mapping = OdaProductMapping.ForProduct(161, "Revet ost", 300, Unit.G, true);

        await Assert.That(mapping.SuggestedPackCount(1.5, Unit.Pack)).IsEqualTo(2);
        await Assert.That(mapping.SuggestedPackCount(0.75, Unit.Pack)).IsEqualTo(1);
    }

    // Verifies that incompatible dimensions and missing pack data yield no suggestion.
    [Test]
    public async Task ReturnsNullWhenUnitsCannotBeReconciled()
    {
        var weightPack = OdaProductMapping.ForProduct(4, "Olje", 500, Unit.Ml, true);
        var piecePack = OdaProductMapping.ForProduct(5, "Egg", 18, Unit.Pcs, true);
        var notAvailable = OdaProductMapping.NotAvailable();

        await Assert.That(weightPack.SuggestedPackCount(200, Unit.G)).IsNull();
        await Assert.That(piecePack.SuggestedPackCount(300, Unit.G)).IsNull();
        await Assert.That(notAvailable.SuggestedPackCount(1, Unit.Pcs)).IsNull();
    }

    // Verifies that confirmation is recorded only when requested.
    [Test]
    public async Task RecordsConfirmationOnlyWhenRequested()
    {
        var confirmed = OdaProductMapping.ForProduct(1, "A", 1, Unit.Pcs, true);
        var suggested = OdaProductMapping.ForProduct(1, "A", 1, Unit.Pcs, false);

        await Assert.That(confirmed.IsConfirmed).IsTrue();
        await Assert.That(suggested.IsConfirmed).IsFalse();
        await Assert.That(OdaProductMapping.NotAvailable().Availability).IsEqualTo(OdaAvailability.NotAvailable);
    }
}
