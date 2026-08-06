using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Autodesk.DesignScript.Runtime;

namespace Interlude.Conditions;

/// <summary>
/// Lets a computed value be written as a bare scalar instead of an object.
///
/// <c>"value": "{prefix}{name}"</c> and
/// <c>"value": { "$type": "format", "template": "{prefix}{name}" }</c> mean the same thing. The
/// short form exists because interpolation is what nearly every computed value in a hand-written
/// form actually is, and two levels of wrapper around one string is most of what made a live
/// preview feel like work.
///
/// THE BRACE RULE. A bare string with a brace in it is a template; one without is a field key.
/// That is one rule covering both readings, and it is the rule the node layer already follows —
/// <c>Compute.Arithmetic("quantity", "Multiply", "unitPrice")</c> has meant "the field called
/// quantity" since the first release. Had this converter read every bare string as a template,
/// the same <c>"left": "quantity"</c> would mean the field in a graph and the literal word in the
/// file that graph saved, which is a worse thing to explain than a rule about braces.
///
/// Applied per property rather than to <see cref="ComputedValue"/> itself, which matters: a
/// converter registered for the type would take over polymorphic dispatch and have to
/// re-implement it. As a property attribute it can hand the object case straight back to the
/// serializer, because the type-level lookup that resolves <c>$type</c> does not see it.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class ComputedValueConverter : JsonConverter<ComputedValue>
{
    public override ComputedValue? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                string text = reader.GetString() ?? string.Empty;

                // A brace is the only thing that could make this text a template, so it is also
                // the only thing worth testing for. A key never contains one: keys are slugs.
                return text.Contains('{')
                    ? new FormatComputed { Template = text }
                    : new FieldComputed { Key = text };

            // A bare number or boolean is the constant it looks like, which is what makes
            // "left": "{quantity}", "right": 2 read the way an author would write it by hand.
            case JsonTokenType.Number:
                return new ConstantComputed { Value = reader.GetDouble() };

            case JsonTokenType.True:
            case JsonTokenType.False:
                return new ConstantComputed { Value = reader.GetBoolean() };

            case JsonTokenType.Null:
                return null;

            default:
                return JsonSerializer.Deserialize<ComputedValue>(ref reader, options);
        }
    }

    /// <summary>
    /// Always writes the long form, including for a plain template.
    ///
    /// Writing the short form would round-trip a hand-authored file more faithfully, and it was
    /// tempting for exactly that reason. It was not worth it: <c>Form.ToJson</c> already expands
    /// every other default, so the file a graph writes has never looked like the file a person
    /// wrote, and emitting a string where every previous release emitted an object would make
    /// every form carrying a computed value unreadable by those releases — a cost paid by forms
    /// that have nothing to do with this feature.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, ComputedValue value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value, options);
}
