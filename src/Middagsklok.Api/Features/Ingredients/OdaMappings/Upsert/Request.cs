using System.Text.Json.Serialization;

namespace Middagsklok.Api.Features.Ingredients.OdaMappings.Upsert;

// Either a concrete product (productId, productName, packQuantity, packUnit) or notAvailable = true.
public sealed record Request(
    [property: JsonPropertyName("productId")] int? ProductId,
    [property: JsonPropertyName("productName")] string? ProductName,
    [property: JsonPropertyName("packQuantity")] double? PackQuantity,
    [property: JsonPropertyName("packUnit")] string? PackUnit,
    [property: JsonPropertyName("confirmed")] bool Confirmed = true,
    [property: JsonPropertyName("notAvailable")] bool NotAvailable = false);
