using System.Text;

namespace Middagsklok.Api.Domain.Ingredient;

// Folds ingredient names down to a key that survives the wording differences seen in recipe prose,
// so that "poteter", "Potet" and "Hvitløksfedd, finhakket" no longer become separate records.
public static class IngredientNameMatching
{
    // Everything from the first of these onwards is a qualifier, not part of the product name.
    private static readonly char[] QualifierStarts = [',', '(', ';'];

    // Phrases that introduce how an ingredient is used or prepared rather than what it is.
    private static readonly string[] UsagePhrases = [" til ", " uten "];

    // Norwegian plural and definite endings, longest first so that "ene" wins over "en".
    private static readonly string[] Endings = ["ene", "er", "en", "e"];

    // Shorter stems fold too aggressively and start matching unrelated products.
    private const int MinimumStemLength = 3;

    // Builds the loose key two names must share to be treated as the same product.
    public static string MatchKey(string? name)
    {
        var collapsed = Collapse(name);

        if (collapsed.Length == 0)
        {
            return string.Empty;
        }

        var withoutQualifier = TrimFrom(collapsed, QualifierStarts);
        var withoutUsage = TrimFromUsagePhrase(withoutQualifier);
        var stem = TrimEnding(Collapse(withoutUsage));

        return stem;
    }

    // Trims, lowercases and collapses runs of whitespace into single spaces.
    private static string Collapse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Normalize(NormalizationForm.FormC).ToLowerInvariant();
        var builder = new StringBuilder(normalized.Length);
        var previousWasWhitespace = false;

        foreach (var character in normalized)
        {
            if (char.IsWhiteSpace(character))
            {
                previousWasWhitespace = builder.Length > 0;
                continue;
            }

            if (previousWasWhitespace)
            {
                builder.Append(' ');
                previousWasWhitespace = false;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    // Cuts the value at the first qualifier character.
    private static string TrimFrom(string value, char[] separators)
    {
        var index = value.IndexOfAny(separators);

        return index < 0 ? value : value[..index];
    }

    // Cuts the value at the first usage phrase.
    private static string TrimFromUsagePhrase(string value)
    {
        var cut = value.Length;

        foreach (var phrase in UsagePhrases)
        {
            var index = value.IndexOf(phrase, StringComparison.Ordinal);

            if (index >= 0 && index < cut)
            {
                cut = index;
            }
        }

        return value[..cut];
    }

    // Removes a plural or definite ending when a long enough stem remains.
    private static string TrimEnding(string value)
    {
        foreach (var ending in Endings)
        {
            if (!value.EndsWith(ending, StringComparison.Ordinal))
            {
                continue;
            }

            var stem = value[..^ending.Length];

            if (stem.Length >= MinimumStemLength)
            {
                return stem;
            }
        }

        return value;
    }
}
