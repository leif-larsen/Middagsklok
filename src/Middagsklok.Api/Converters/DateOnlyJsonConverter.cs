using System.Text.Json;
using System.Text.Json.Serialization;

namespace Middagsklok.Api.Converters;

public class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException("Date value cannot be null or empty.");
        }

        if (!DateOnly.TryParse(value, out var date))
        {
            throw new JsonException($"Invalid date format: '{value}'. Expected format: YYYY-MM-DD.");
        }

        return date;
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-dd"));
    }
}
