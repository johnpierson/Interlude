using System.Collections.Generic;
using System.Linq;
using Interlude.Conditions;
using Interlude.Model;
using Interlude.Runtime;
using Interlude.Serialization;
using Xunit;

namespace Interlude.Tests;

/// <summary>
/// The preview element: a derived value shown back to the user and belonging to nobody.
///
/// The behaviour worth protecting is mostly what a preview does *not* do — produce an answer,
/// accept one, or take part in validation — because those are the three things that made the
/// read-only-computed-field arrangement it replaces feel wrong.
/// </summary>
public class PreviewTests
{
    private static PreviewElement Preview(string template, string? label = "Preview")
        => new() { Label = label, Value = new FormatComputed { Template = template } };

    [Fact]
    public void A_preview_settles_before_the_form_is_shown()
    {
        PreviewElement preview = Preview("{prefix}{name}");
        FormSession session = new(TestForms.Form(
            TestForms.Text("prefix", "WIP_"),
            TestForms.Text("name", "L1 - Floor Plan"),
            preview));

        // Not after the first keystroke: the first frame has to be right, or every form opens
        // showing a preview of nothing.
        Assert.Equal("WIP_L1 - Floor Plan", session.GetState(preview).Value);
    }

    [Fact]
    public void A_preview_follows_the_fields_it_reads()
    {
        PreviewElement preview = Preview("{prefix}{name}{suffix}");
        FormSession session = new(TestForms.Form(
            TestForms.Text("prefix", "WIP_"),
            TestForms.Text("name", "L1"),
            TestForms.Text("suffix"),
            preview));

        session.SetValue("suffix", " (draft)");

        Assert.Equal("WIP_L1 (draft)", session.GetState(preview).Value);
    }

    /// <summary>
    /// One edit, one batch, and the preview is in it. This is what makes the preview live in the
    /// window: the renderer's whole job is to apply the batch.
    /// </summary>
    [Fact]
    public void An_edit_reports_the_preview_in_the_same_batch()
    {
        PreviewElement preview = Preview("{name}");
        FormSession session = new(TestForms.Form(TestForms.Text("name"), preview));

        List<ElementStateChange> received = new();
        session.Changed += (_, e) => received.AddRange(e.Changes);

        session.SetValue("name", "Ada");

        Assert.Contains(
            received,
            change => ReferenceEquals(change.Element, preview) && change.Includes(StateChangeKind.Value));
    }

    [Fact]
    public void A_preview_contributes_nothing_to_the_results()
    {
        PreviewElement preview = Preview("{name}");
        FormSession session = new(TestForms.Form(TestForms.Text("name", "Ada"), preview));

        FormResult result = session.BuildResult(true, FormButtonNames.Submit);

        Assert.Equal(new[] { "name" }, result.Values.Keys.OrderBy(key => key).ToArray());
    }

    /// <summary>A preview has no key, so there is nothing for a graph or a condition to name.</summary>
    [Fact]
    public void A_preview_contributes_nothing_to_the_defaults()
    {
        PreviewElement preview = Preview("{name}");
        FormSession session = new(TestForms.Form(TestForms.Text("name", "Ada"), preview));

        Assert.Equal(new[] { "name" }, session.Defaults.Keys.OrderBy(key => key).ToArray());
    }

    /// <summary>
    /// An empty preview is not an unanswered question. Requiring one would be unsatisfiable,
    /// because there is no control for the user to satisfy it with.
    /// </summary>
    [Fact]
    public void An_empty_preview_does_not_block_submission()
    {
        PreviewElement preview = new()
        {
            Label = "Preview",
            Value = new FormatComputed { Template = "{name}" },
            RequiredIf = ConstantCondition.True,
        };

        FormSession session = new(TestForms.Form(TestForms.Text("name"), preview));

        Assert.True(session.Validate());
    }

    [Fact]
    public void A_preview_can_be_hidden_by_a_condition()
    {
        PreviewElement preview = new()
        {
            Label = "Preview",
            Value = new FormatComputed { Template = "{name}" },
            VisibleIf = new ComparisonCondition { Key = "show", Operator = ComparisonOperator.IsChecked },
        };

        FormSession session = new(TestForms.Form(
            TestForms.Text("name", "Ada"),
            TestForms.Check("show"),
            preview));

        Assert.False(session.GetState(preview).IsVisible);
        session.SetValue("show", true);
        Assert.True(session.GetState(preview).IsVisible);
    }

    /// <summary>
    /// A preview reads a computed field, which reads two entered ones. Everything has to settle
    /// in a single pass, or the preview shows the answer from before the keystroke.
    /// </summary>
    [Fact]
    public void A_preview_reading_a_computed_field_settles_in_one_pass()
    {
        PreviewElement preview = Preview("Total: {total:F2}");

        FormSession session = new(TestForms.Form(
            TestForms.Number("quantity", 2d),
            TestForms.Number("price", 10d),
            new NumericElement
            {
                Key = "total",
                Label = "Total",
                Computed = new ArithmeticComputed
                {
                    Operator = ArithmeticOperator.Multiply,
                    Left = new FieldComputed { Key = "quantity" },
                    Right = new FieldComputed { Key = "price" },
                },
            },
            preview));

        session.SetValue("quantity", 3d);

        Assert.Equal("Total: 30.00", session.GetState(preview).Value);
    }

    [Fact]
    public void A_preview_reading_an_unknown_key_is_reported()
    {
        FormSession session = new(TestForms.Form(TestForms.Text("name"), Preview("{nmae}")));

        Assert.Contains(session.Warnings, warning => warning.Contains("nmae"));
    }

    // ---- Format specifiers -------------------------------------------------

    [Theory]
    [InlineData("{n:000}", 7, "007")]
    [InlineData("{n:F2}", 7, "7.00")]
    [InlineData("{n}", 7, "7")]
    public void A_placeholder_may_carry_a_format_specifier(string template, int value, string expected)
    {
        PreviewElement preview = Preview(template);
        FormSession session = new(TestForms.Form(
            new IntegerElement { Key = "n", Label = "n", DefaultValue = value },
            preview));

        Assert.Equal(expected, session.GetState(preview).Value);
    }

    /// <summary>
    /// Without a specifier this reads "0.30000000000000004", which is correct, useless, and the
    /// reason the feature exists.
    /// </summary>
    [Fact]
    public void A_specifier_tames_the_way_doubles_print()
    {
        PreviewElement preview = Preview("{a:0.##}");
        FormSession session = new(TestForms.Form(
            TestForms.Number("a", 0.1d + 0.2d),
            preview));

        Assert.Equal("0.3", session.GetState(preview).Value);
    }

    /// <summary>
    /// Interlude invents no formatting rules of its own: the specifier goes to .NET, and an
    /// unrecognised one is a custom format whose literal characters come out as themselves. This
    /// is what <c>string.Format("{0:Z9Z9}", 7)</c> does, and someone reaching for a specifier
    /// already knows that language.
    /// </summary>
    [Fact]
    public void An_unrecognised_specifier_follows_dotnet_formatting()
    {
        PreviewElement preview = Preview("{n:Z9Z9}");
        FormSession session = new(TestForms.Form(
            new IntegerElement { Key = "n", Label = "n", DefaultValue = 7 },
            preview));

        Assert.Equal("Z9Z9", session.GetState(preview).Value);
    }

    /// <summary>
    /// Templates are edited live and are invalid most of the way through being typed, so a
    /// specifier that .NET rejects outright shows the plain value rather than taking the form
    /// down with an exception from inside a keystroke.
    /// </summary>
    [Fact]
    public void A_specifier_that_throws_falls_back_to_the_plain_value()
    {
        PreviewElement preview = Preview("{when:%}");
        FormSession session = new(TestForms.Form(
            new DatePickerElement
            {
                Key = "when",
                Label = "when",
                DefaultValue = new System.DateTime(2026, 8, 5),
            },
            preview));

        Assert.Equal("2026-08-05T00:00:00.0000000", session.GetState(preview).Value);
    }

    /// <summary>A value with nothing to format a specifier with simply ignores it.</summary>
    [Fact]
    public void A_specifier_on_text_is_ignored()
    {
        PreviewElement preview = Preview("{name:F2}");
        FormSession session = new(TestForms.Form(TestForms.Text("name", "Ada"), preview));

        Assert.Equal("Ada", session.GetState(preview).Value);
    }

    /// <summary>
    /// The dependency graph is keyed on field names. A specifier that leaked into one would order
    /// the form against a field that does not exist.
    /// </summary>
    [Fact]
    public void A_specifier_is_not_part_of_the_dependency()
    {
        FormatComputed format = new() { Template = "{total:F2} of {count:000}" };

        Assert.Equal(new[] { "total", "count" }, format.DependsOn().ToArray());
    }

    // ---- The bare-string form ----------------------------------------------

    [Fact]
    public void A_bare_string_is_read_as_a_template()
    {
        FormDefinition form = FormJson.Deserialize("""
            {
              "schemaVersion": 1,
              "title": "Rename",
              "elements": [
                { "$type": "textBox", "key": "name", "defaultValue": "L1" },
                { "$type": "preview", "label": "New name", "value": "WIP_{name}" }
              ]
            }
            """);

        FormSession session = new(form);
        PreviewElement preview = form.AllElements().OfType<PreviewElement>().Single();

        Assert.IsType<FormatComputed>(preview.Value);
        Assert.Equal("WIP_{name}", ((FormatComputed)preview.Value!).Template);
        Assert.Equal("WIP_L1", session.GetState(preview).Value);
    }

    /// <summary>
    /// The shorthand has to work in the nested slots too. Those are where the awkward JSON was:
    /// a conditional preview meant two <c>$type: format</c> wrappers inside a third object.
    /// </summary>
    [Fact]
    public void A_bare_string_is_read_as_a_template_inside_a_conditional()
    {
        FormDefinition form = FormJson.Deserialize("""
            {
              "schemaVersion": 1,
              "title": "Rename",
              "elements": [
                { "$type": "textBox", "key": "name", "defaultValue": "L1" },
                { "$type": "checkBox", "key": "shout" },
                { "$type": "preview", "label": "New name",
                  "value": {
                    "$type": "conditional",
                    "condition": { "$type": "comparison", "key": "shout", "operator": "isChecked" },
                    "ifTrue": "{name}!",
                    "ifFalse": "{name}"
                  } }
              ]
            }
            """);

        FormSession session = new(form);
        PreviewElement preview = form.AllElements().OfType<PreviewElement>().Single();

        Assert.Equal("L1", session.GetState(preview).Value);
        session.SetValue("shout", true);
        Assert.Equal("L1!", session.GetState(preview).Value);
    }

    /// <summary>The long form still reads, so no checked-in form stops working.</summary>
    [Fact]
    public void The_long_form_still_reads()
    {
        FormDefinition form = FormJson.Deserialize("""
            {
              "schemaVersion": 1,
              "title": "Rename",
              "elements": [
                { "$type": "textBox", "key": "name", "defaultValue": "L1" },
                { "$type": "preview", "label": "New name",
                  "value": { "$type": "format", "template": "WIP_{name}" } }
              ]
            }
            """);

        PreviewElement preview = form.AllElements().OfType<PreviewElement>().Single();

        Assert.Equal("WIP_{name}", Assert.IsType<FormatComputed>(preview.Value).Template);
    }

    /// <summary>
    /// The brace rule, in the slot where it matters most: a bare name is the field, which is what
    /// <c>Compute.Arithmetic("quantity", "Multiply", "unitPrice")</c> has always meant on a port.
    /// A string that meant one thing in a graph and another in the file that graph saved would be
    /// a genuinely nasty thing to debug.
    /// </summary>
    [Fact]
    public void A_bare_name_in_an_arithmetic_slot_is_the_field()
    {
        FormDefinition form = FormJson.Deserialize("""
            {
              "schemaVersion": 1,
              "title": "Arithmetic",
              "elements": [
                { "$type": "numeric", "key": "quantity", "defaultValue": 6.0 },
                { "$type": "numeric", "key": "doubled",
                  "computed": { "$type": "arithmetic", "operator": "multiply",
                                "left": "quantity", "right": 2.0 } }
              ]
            }
            """);

        FormSession session = new(form);

        Assert.Equal(12d, session.GetValue("doubled"));
    }

    /// <summary>The JSON reading and the node reading of the same string must not disagree.</summary>
    [Theory]
    [InlineData("quantity")]
    [InlineData("{quantity}")]
    [InlineData("{quantity} each")]
    public void The_brace_rule_reads_the_same_in_JSON_as_on_a_port(string text)
    {
        FormDefinition form = FormJson.Deserialize($$"""
            {
              "schemaVersion": 1,
              "title": "Brace rule",
              "elements": [{ "$type": "preview", "label": "p", "value": "{{text}}" }]
            }
            """);

        ComputedValue fromJson = form.AllElements().OfType<PreviewElement>().Single().Value!;
        ComputedValue fromPort = ((PreviewElement)Interlude.Layout.Preview("p", text)).Value!;

        Assert.Equal(fromPort, fromJson);
    }

    [Fact]
    public void A_preview_survives_a_round_trip()
    {
        FormDefinition original = new FormDefinition
        {
            Title = "Rename",
            Elements = new FormElement[]
            {
                TestForms.Text("name", "L1"),
                new PreviewElement
                {
                    Label = "New name",
                    Value = new FormatComputed { Template = "WIP_{name}" },
                    Placeholder = "Nothing to show yet",
                    IsMonospaced = true,
                },
            },
        }.WithResolvedKeys();

        FormDefinition restored = FormJson.Deserialize(FormJson.Serialize(original));
        PreviewElement preview = restored.AllElements().OfType<PreviewElement>().Single();

        Assert.Equal("New name", preview.Label);
        Assert.Equal("Nothing to show yet", preview.Placeholder);
        Assert.True(preview.IsMonospaced);
        Assert.Equal("WIP_{name}", Assert.IsType<FormatComputed>(preview.Value).Template);
    }

    /// <summary>
    /// Every previous release reads a computed value as an object. Emitting the shorthand would
    /// make forms that have nothing to do with previews unreadable by those releases.
    /// </summary>
    [Fact]
    public void A_template_is_written_in_the_long_form()
    {
        FormDefinition form = new FormDefinition
        {
            Title = "Rename",
            Elements = new FormElement[] { Preview("WIP_{name}") },
        }.WithResolvedKeys();

        string json = FormJson.Serialize(form);

        Assert.Contains("\"$type\": \"format\"", json);
        Assert.Contains("\"template\": \"WIP_{name}\"", json);
    }
}
