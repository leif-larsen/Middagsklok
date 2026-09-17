namespace Middagsklok.Api.Domain.Ingredient;

// Records which Oda product an ingredient resolves to, or that Oda does not stock it.
public class OdaProductMapping
{
    // Required by EF Core.
    private OdaProductMapping()
    {
    }

    private OdaProductMapping(
        OdaAvailability availability,
        int? odaProductId,
        string? odaProductName,
        double? packQuantity,
        Unit? packUnit,
        DateTime? confirmedAt)
    {
        Availability = availability;
        OdaProductId = odaProductId;
        OdaProductName = odaProductName?.Trim();
        PackQuantity = packQuantity;
        PackUnit = packUnit;
        ConfirmedAt = confirmedAt;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public OdaAvailability Availability { get; private set; }
    public int? OdaProductId { get; private set; }

    // Snapshot of the product name at mapping time, for display and drift detection.
    public string? OdaProductName { get; private set; }
    public double? PackQuantity { get; private set; }
    public Unit? PackUnit { get; private set; }

    // Null means the mapping was auto-suggested and has not been confirmed by a person.
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public bool IsConfirmed => ConfirmedAt is not null;

    // Creates a mapping to a concrete Oda product.
    public static OdaProductMapping ForProduct(
        int odaProductId,
        string odaProductName,
        double packQuantity,
        Unit packUnit,
        bool confirmed) =>
        new(
            OdaAvailability.Available,
            odaProductId,
            odaProductName,
            packQuantity,
            packUnit,
            confirmed ? DateTime.UtcNow : null);

    // Creates a mapping that records the ingredient as not stocked at Oda.
    public static OdaProductMapping NotAvailable() =>
        new(OdaAvailability.NotAvailable, null, null, null, null, DateTime.UtcNow);

    // Suggests how many packages cover the amount, or null when the units cannot be reconciled.
    public int? SuggestedPackCount(double amount, Unit unit)
    {
        if (Availability is not OdaAvailability.Available || PackQuantity is not > 0 || PackUnit is null)
        {
            return null;
        }

        if (amount <= 0)
        {
            return 0;
        }

        // A pack fraction already expresses "this many retail packages".
        if (unit is Unit.Pack)
        {
            return CeilToInt(amount);
        }

        var packUnit = PackUnit.Value;
        var packQuantity = PackQuantity.Value;

        // Pieces against a weight- or volume-based pack do not convert; one piece is one package.
        if (unit is Unit.Pcs && packUnit is not Unit.Pcs)
        {
            return CeilToInt(amount);
        }

        var normalizedAmount = ToBaseUnit(amount, unit);
        var normalizedPack = ToBaseUnit(packQuantity, packUnit);

        if (normalizedAmount is null || normalizedPack is null
            || normalizedAmount.Value.Dimension != normalizedPack.Value.Dimension)
        {
            return null;
        }

        var count = CeilToInt(normalizedAmount.Value.Value / normalizedPack.Value.Value);

        return count;
    }

    // Expresses an amount in its dimension's base unit so compatible units can be compared.
    private static (UnitDimension Dimension, double Value)? ToBaseUnit(double amount, Unit unit) =>
        unit switch
        {
            Unit.G => (UnitDimension.Weight, amount),
            Unit.Kg => (UnitDimension.Weight, amount * 1000),
            Unit.Ml => (UnitDimension.Volume, amount),
            Unit.L => (UnitDimension.Volume, amount * 1000),
            Unit.Pcs => (UnitDimension.Count, amount),
            _ => null
        };

    // Rounds up to a whole package while ignoring floating point noise just above an integer.
    private static int CeilToInt(double value) => (int)Math.Ceiling(Math.Round(value, 6));

    private enum UnitDimension
    {
        Weight,
        Volume,
        Count
    }
}

// Whether Oda stocks something that matches the ingredient.
public enum OdaAvailability
{
    Available,
    NotAvailable
}
