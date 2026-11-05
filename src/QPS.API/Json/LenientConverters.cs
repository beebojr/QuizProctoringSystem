using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QPS.API.Json;

/// <summary>Accepts "09:00", "09:00:00" and "9:00 AM"; always writes "HH:mm:ss".</summary>
public sealed class LenientTimeOnlyConverter : JsonConverter<TimeOnly>
{
    private static readonly string[] Formats = { "HH:mm:ss", "HH:mm", "H:mm", "HH:mm:ss.FFFFFFF", "h:mm tt", "h:mmtt" };

    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString();
        if (text is not null && TimeOnly.TryParseExact(text.Trim(), Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
            return time;
        throw new JsonException($"\"{text}\" is not a time (use HH:mm).");
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
}

/// <summary>Accepts "2026-04-21" and full ISO date-times; always writes "yyyy-MM-dd".</summary>
public sealed class LenientDateOnlyConverter : JsonConverter<DateOnly>
{
    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString();
        if (text is not null && DateOnly.TryParseExact(text.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date;
        if (text is not null && DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
            return DateOnly.FromDateTime(dt);
        throw new JsonException($"\"{text}\" is not a date (use yyyy-MM-dd).");
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
}
