using Middagsklok.Api.Domain.Ingredient;
using TUnit.Assertions;
using TUnit.Core;

namespace Middagsklok.Tests.Domain.Ingredient;

public sealed class IngredientNameMatchingTests
{
    // Verifies that the wordings observed in the imported recipe prose fold onto the canonical entry.
    [Test]
    [Arguments("poteter", "Potet")]
    [Arguments("hvitløksfedd", "Hvitløksfedd, finhakket")]
    [Arguments("Matfløte", "Matfløte (15–20%)")]
    [Arguments("smør", "smør til steking av brød")]
    [Arguments("gressløk", "gressløk (til pynt, valgfritt)")]
    [Arguments("Sitronsaft", "Sitronsaft (ca. ½ sitron)")]
    [Arguments("Laksefilet", "Laksefilet uten skinn og bein")]
    [Arguments("Ost", "oster")]
    public async Task FoldsWordingVariantsOntoTheSameKey(string canonical, string variant)
    {
        var canonicalKey = IngredientNameMatching.MatchKey(canonical);
        var variantKey = IngredientNameMatching.MatchKey(variant);

        await Assert.That(variantKey).IsEqualTo(canonicalKey);
    }

    // Verifies that distinct products are not folded together.
    [Test]
    [Arguments("Potet", "Søtpotet")]
    [Arguments("Laks", "Laksefilet")]
    [Arguments("Kylling", "Kyllingfilet")]
    [Arguments("Matfløte", "Fløte")]
    [Arguments("grønnsaksbuljong", "grønnsakskraft")]
    [Arguments("Smør", "Smørbrød")]
    public async Task KeepsDistinctProductsApart(string first, string second)
    {
        var firstKey = IngredientNameMatching.MatchKey(first);
        var secondKey = IngredientNameMatching.MatchKey(second);

        await Assert.That(firstKey).IsNotEqualTo(secondKey);
    }

    // Verifies that short names are not stripped down to an ambiguous stem.
    [Test]
    [Arguments("Ris", "ris")]
    [Arguments("Egg", "egg")]
    [Arguments("Salt", "salt")]
    public async Task LeavesShortNamesIntact(string name, string expected)
    {
        var key = IngredientNameMatching.MatchKey(name);

        await Assert.That(key).IsEqualTo(expected);
    }

    // Verifies that blank input produces an empty key rather than throwing.
    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task ReturnsEmptyKeyForBlankNames(string? name)
    {
        var key = IngredientNameMatching.MatchKey(name);

        await Assert.That(key).IsEqualTo(string.Empty);
    }
}
