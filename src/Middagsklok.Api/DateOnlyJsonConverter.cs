using System.Text.Json;
using System.Text.Json.Serialization;

namespace Middagsklok.Api;

public class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    private const string Format = "yyyy-MM-dd";

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
            throw new JsonException("Date value cannot be null or empty.");
        
        if (!DateOnly.TryParseExact(value, Format, out var date))
            throw new JsonException($"Date must be in {Format} format.");
        
        return date;
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Format));
    }
}
