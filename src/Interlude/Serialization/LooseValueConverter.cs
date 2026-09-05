using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Autodesk.DesignScript.Runtime;
using Interlude.Conditions;
using Interlude.Model;

namespace Interlude.Serialization;

/// <summary>
/// Reads and writes the loosely-typed values a form carries: default values, condition operands
/// and option values, all declared as <c>object</c> because a graph can put anything there.
///
/// Two things this converter is careful about:
///
/// Numbers keep their type. A double that happens to be whole is written as <c>3.0</c> rather
/// than <c>3</c>, so a form's JSON round-trips to an equal form instead of quietly turning every
/// slider default into an integer.
///
/// Objects JSON cannot represent — a Revit wall on a dropdown option, say — are written as an
/// <c>$opaque</c> marker holding their text. This is lossy and deliberately visible: a form
/// bound to live model elements is not a portable document, and pretending otherwise would
/// produce a file that loads without complaint and selects the wrong thing.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class LooseValueConverter : JsonConverter<object?>
{
    private const string DateMarker = "$date";
    private const string ColorMarker = "$color";
    private const string OpaqueMarker = "$opaque";

    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => ReadValue(ref reader);

    public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                return;

            case bool boolean:
                writer.WriteBooleanValue(boolean);
                return;

            case string text:
                writer.WriteStringValue(text);
                return;

            case int or long or short or byte or sbyte or uint or ushort:
                writer.WriteNumberValue(Convert.ToInt64(value, CultureInfo.InvariantCulture));
                return;

            case double or float or decimal:
                WriteRealNumber(writer, Convert.ToDouble(value, CultureInfo.InvariantCulture));
                return;

            case DateTime date:
                writer.WriteStartObject();
                writer.WriteString(DateMarker, date.ToString("o", CultureInfo.InvariantCulture));
                writer.WriteEndObject();
                return;

            case RgbColor color:
                writer.WriteStartObject();
                writer.WriteString(ColorMarker, color.ToHex());
                writer.WriteEndObject();
                return;

            default:
                if (value is IDictionary dictionary)
                {
                    writer.WriteStartObject();
                    foreach (DictionaryEntry entry in dictionary)
                    {
                        writer.WritePropertyName(ValueOps.ToStringInvariant(entry.Key));
                        Write(writer, entry.Value, options);
                    }

                    writer.WriteEndObject();
                    return;
                }

                if (ValueOps.TryAsSequence(value, out IReadOnlyList<object?> items))
                {
                    writer.WriteStartArray();
                    foreach (object? item in items)
                    {
                        Write(writer, item, options);
                    }

                    writer.WriteEndArray();
                    return;
                }

                writer.WriteStartObject();
                writer.WriteString(OpaqueMarker, ValueOps.ToStringInvariant(value));
                writer.WriteEndObject();
                return;
        }
    }

    private static void WriteRealNumber(Utf8JsonWriter writer, double number)
    {
        if (double.IsNaN(number) || double.IsInfinity(number))
        {
            // JSON has no way to say NaN. Recording it as text at least preserves the intent.
            writer.WriteStringValue(number.ToString("R", CultureInfo.InvariantCulture));
            return;
        }

        if (number == Math.Floor(number) && Math.Abs(number) < 1e15)
        {
            // The whole point: keep whole doubles distinguishable from integers on the way back.
            writer.WriteRawValue(
                number.ToString("0.0##############", CultureInfo.InvariantCulture),
                skipInputValidation: true);
            return;
        }

        writer.WriteNumberValue(number);
    }

    private static object? ReadValue(ref Utf8JsonReader reader)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;

            case JsonTokenType.True:
                return true;

            case JsonTokenType.False:
                return false;

            case JsonTokenType.String:
                return reader.GetString();

            case JsonTokenType.Number:
                // TryGetInt32 rejects "3.0", which is exactly how the written form of a whole
                // double survives the round trip as a double.
                if (reader.TryGetInt32(out int integer))
                {
                    return integer;
                }

                if (reader.TryGetInt64(out long longInteger))
                {
                    return longInteger;
                }

                return reader.GetDouble();

            case JsonTokenType.StartArray:
                return ReadArray(ref reader);

            case JsonTokenType.StartObject:
                return ReadObject(ref reader);

            default:
                throw new JsonException($"Unexpected token '{reader.TokenType}' while reading a form value.");
        }
    }

    private static List<object?> ReadArray(ref Utf8JsonReader reader)
    {
        List<object?> items = new();

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            items.Add(ReadValue(ref reader));
        }

        return items;
    }

    private static object? ReadObject(ref Utf8JsonReader reader)
    {
        Dictionary<string, object?> members = new(StringComparer.Ordinal);

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            string name = reader.GetString() ?? string.Empty;
            reader.Read();
            members[name] = ReadValue(ref reader);
        }

        if (members.Count == 1)
        {
            if (members.TryGetValue(DateMarker, out object? date) && date is string dateText &&
                DateTime.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsedDate))
            {
                return parsedDate;
            }

            if (members.TryGetValue(ColorMarker, out object? color) && color is string colorText &&
                RgbColor.TryParse(colorText, out RgbColor parsedColor))
            {
                return parsedColor;
            }

            if (members.TryGetValue(OpaqueMarker, out object? opaque))
            {
                // All that survived of the original object was its text, and saying so is more
                // useful than resurrecting a dictionary nothing knows how to use.
                return opaque as string ?? string.Empty;
            }
        }

        return members;
    }
}
