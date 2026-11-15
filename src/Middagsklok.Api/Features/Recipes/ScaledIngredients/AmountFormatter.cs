using System.Globalization;
using Middagsklok.Api.Domain.Ingredient;

namespace Middagsklok.Api.Features.Recipes.ScaledIngredients;

// Renders scaled ingredient amounts the way a cook reads them: Norwegian decimal comma,
// volumes stepping up from ml to dl to l, weights from g to kg, and countable units in quarters.
internal static class AmountFormatter
{
    private const double MlPerDl = 100;
    private const double MlPerL = 1000;
    private const double GramsPerKg = 1000;

    // Formats an amount in the given unit, possibly converting it to a more readable unit.
    public static DisplayAmount Format(double amount, Unit unit) =>
        unit switch
        {
            Unit.Ml => FormatVolume(amount),
            Unit.L => FormatVolume(amount * MlPerL),
            Unit.G => FormatWeight(amount),
            Unit.Kg => FormatWeight(amount * GramsPerKg),
            Unit.Pcs => new DisplayAmount(FormatQuarters(amount), "stk"),
            Unit.Pack => new DisplayAmount(FormatQuarters(amount), "pk"),
            _ => new DisplayAmount(FormatDecimal(amount, 2), unit.ToString().ToLowerInvariant())
        };

    // Keeps small volumes in ml, shows mid-range volumes in dl and large ones in litres.
    private static DisplayAmount FormatVolume(double ml)
    {
        var roundedMl = RoundBaseUnit(ml);
        if (roundedMl < MlPerDl)
        {
            return new DisplayAmount(FormatDecimal(roundedMl, 1), "ml");
        }

        var dl = Round(ml / MlPerDl, 1);
        if (dl < MlPerL / MlPerDl)
        {
            return new DisplayAmount(FormatDecimal(dl, 1), "dl");
        }

        return new DisplayAmount(FormatDecimal(dl * MlPerDl / MlPerL, 2), "l");
    }

    // Keeps weights below a kilo in grams and switches to kg above that.
    private static DisplayAmount FormatWeight(double grams)
    {
        var roundedGrams = RoundBaseUnit(grams);
        if (roundedGrams < GramsPerKg)
        {
            return new DisplayAmount(FormatDecimal(roundedGrams, 1), "g");
        }

        return new DisplayAmount(FormatDecimal(roundedGrams / GramsPerKg, 2), "kg");
    }

    // Rounds ml or g to whole units, keeping one decimal for amounts under ten.
    private static double RoundBaseUnit(double value) =>
        Round(value, Math.Abs(value) < 10 ? 1 : 0);

    // Rounds countable amounts to the nearest quarter, never showing a positive amount as zero.
    private static string FormatQuarters(double amount)
    {
        var quarters = Round(amount * 4, 0);
        if (quarters == 0 && amount > 0)
        {
            quarters = 1;
        }

        var whole = (int)Math.Floor(quarters / 4);
        var fraction = ((int)quarters % 4) switch
        {
            1 => "¼",
            2 => "½",
            3 => "¾",
            _ => string.Empty
        };

        if (whole == 0 && fraction.Length > 0)
        {
            return fraction;
        }

        return $"{whole.ToString(CultureInfo.InvariantCulture)}{fraction}";
    }

    // Formats a number with up to the given decimals, trailing zeros trimmed, using a decimal comma.
    private static string FormatDecimal(double value, int decimals)
    {
        var format = decimals > 0 ? $"0.{new string('#', decimals)}" : "0";
        var formatted = Round(value, decimals).ToString(format, CultureInfo.InvariantCulture);

        return formatted.Replace('.', ',');
    }

    // Rounds half away from zero so 0.5 ml never collapses to 0.
    private static double Round(double value, int decimals) =>
        Math.Round(value, decimals, MidpointRounding.AwayFromZero);
}

internal readonly record struct DisplayAmount(string Amount, string Unit);
