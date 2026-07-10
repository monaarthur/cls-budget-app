using System.Text.Json;
using System.Text.Json.Serialization;

namespace CLS.Budget.Application.Common.Serialization;

public sealed class DateOnlyUtcJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException("Date value is required.");
        }

        if (!DateTime.TryParse(value, out var parsed))
        {
            throw new JsonException($"Could not parse date value '{value}'.");
        }

        return DateTime.SpecifyKind(parsed.ToUniversalTime().Date, DateTimeKind.Utc);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToUniversalTime().ToString("yyyy-MM-dd"));
    }
}

public sealed class NullableDateOnlyUtcJsonConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        return new DateOnlyUtcJsonConverter().Read(ref reader, typeof(DateTime), options);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (!value.HasValue)
        {
            writer.WriteNullValue();
            return;
        }

        new DateOnlyUtcJsonConverter().Write(writer, value.Value, options);
    }
}
