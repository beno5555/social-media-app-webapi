using System.Text.Json;
using System.Text.Json.Serialization;

namespace aspnetproject.Common.Json;

// Writes every DateTime as UTC, with the Z suffix (ADR 0005). A value read back from a datetime2 column has no kind,
// and without the suffix a browser reads it as local time. The value is labeled, never shifted: all code writes UTC.
// Reading is unchanged from the built-in converter.
public class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetDateTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
}
