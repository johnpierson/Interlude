using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Interlude.Conditions;
using Interlude.Model;
using Interlude.Runtime;
using Interlude.Serialization;
using Interlude.Theming;
using Interlude.Validation;
using Xunit;

namespace Interlude.Tests;

public class SerializationTests
{
    /// <summary>A form exercising most of the schema at once.</summary>
    private static FormDefinition RichForm() => new FormDefinition
    {
        Title = "Everything",
        Description = "A form that touches most of the schema.",
        FormId = "tests.everything",
        Theme = new ThemeDefinition
        {
            Mode = AppearanceMode.Dark,
            Accent = RgbColor.Parse("#FF8800"),
            Density = ThemeDensity.Compact,
            CornerRadius = 6d,
        },
        Window = new WindowOptions { Width = 500d, MaxHeight = 700d, IsResizable = false },
        Buttons = new FormButtons { SubmitText = "Go", CancelText = "Stop" },
        Elements = new FormElement[]
        {
            new TextBoxElement
            {
                Key = "name",
                Label = "Name",
                Placeholder = "Your name",
                DefaultValue = "Ada",
                RequiredIf = ConstantCondition.True,
                Rules = new ValidationRule[] { new LengthRule { Minimum = 2, Maximum = 40 } },
            },
            new NumericElement
            {
                Key = "height",
                Label = "Height",
                DefaultValue = 1.75d,
                Minimum = 0d,
                Maximum = 3d,
                Unit = "m",
                Rules = new ValidationRule[] { new RangeRule { Minimum = 0.5d, Maximum = 2.5d } },
            },
            new IntegerElement { Key = "count", Label = "Count", DefaultValue = 3 },
            new DropdownElement
            {
                Key = "mode",
                Label = "Mode",
                Options = OptionItem.Pair(new object?[] { "auto", "manual" }, new[] { "Automatic", "Manual" }),
                DefaultValue = "auto",
            },
            new DatePickerElement { Key = "when", Label = "When", DefaultValue = new DateTime(2026, 3, 14, 9, 30, 0) },
            new ColorPickerElement { Key = "tint", Label = "Tint", DefaultValue = RgbColor.Parse("#3366CC") },
            new GroupBoxElement
            {
                Header = "Advanced",
                VisibleIf = new ComparisonCondition
                {
                    Key = "mode",
                    Operator = ComparisonOperator.Equals,
                    Operand = "manual",
                },
                Children = new FormElement[]
                {
                    new SliderElement { Key = "tolerance", Label = "Tolerance", Minimum = 0d, Maximum = 1d, Step = 0.05d },
                    new CheckBoxElement { Key = "verbose", Content = "Verbose logging" },
                    new TextBoxElement
                    {
                        Key = "summary",
                        Label = "Summary",
                        Computed = new FormatComputed { Template = "{name} at {height}m" },
                    },
                },
            },
            new TabsElement
            {
                Children = new FormElement[]
                {
                    new TabPageElement
                    {
                        Header = "One",
                        Children = new FormElement[] { new LabelElement { Text = "Hello", HeadingLevel = 2 } },
                    },
                    new TabPageElement
                    {
                        Header = "Two",
                        Children = new FormElement[] { new MarkdownElement { Text = "**bold**" } },
                    },
                },
            },
        },
    }.WithResolvedKeys();

    [Fact]
    public void A_form_survives_a_round_trip_unchanged()
    {
        FormDefinition original = RichForm();

        FormDefinition restored = FormJson.Deserialize(FormJson.Serialize(original));

        Assert.Equal(FormJson.Serialize(original), FormJson.Serialize(restored));
    }

    [Fact]
    public void Element_types_are_preserved_across_a_round_trip()
    {
        FormDefinition restored = FormJson.Deserialize(FormJson.Serialize(RichForm()));

        Assert.Equal(
            RichForm().AllElements().Select(element => element.GetType().Name),
            restored.AllElements().Select(element => element.GetType().Name));
    }

    /// <summary>
    /// The reason <see cref="LooseValueConverter"/> writes whole doubles as "3.0": without it,
    /// a slider default of 3.0 comes back as the integer 3 and the form is subtly not the same form.
    /// </summary>
    [Fact]
    public void Whole_doubles_stay_doubles_and_integers_stay_integers()
    {
        FormDefinition form = new()
        {
            Title = "Numbers",
            Elements = new FormElement[]
            {
                new NumericElement { Key = "real", DefaultValue = 3d },
                new IntegerElement { Key = "whole", DefaultValue = 3 },
            },
        };

        FormDefinition restored = FormJson.Deserialize(FormJson.Serialize(form));
        List<InputElement> inputs = restored.Inputs().ToList();

        Assert.IsType<double>(inputs[0].DefaultValue);
        Assert.IsType<int>(inputs[1].DefaultValue);
    }

    [Fact]
    public void Dates_and_colours_survive_as_dates_and_colours()
    {
        FormDefinition restored = FormJson.Deserialize(FormJson.Serialize(RichForm()));
        Dictionary<string, InputElement> inputs = restored.Inputs().ToDictionary(input => input.Key);

        Assert.Equal(new DateTime(2026, 3, 14, 9, 30, 0), Assert.IsType<DateTime>(inputs["when"].DefaultValue));
        Assert.Equal(RgbColor.Parse("#3366CC"), Assert.IsType<RgbColor>(inputs["tint"].DefaultValue));
    }

    [Fact]
    public void Conditions_computed_values_and_rules_survive()
    {
        FormDefinition restored = FormJson.Deserialize(FormJson.Serialize(RichForm()));

        FormElement group = restored.AllElements().OfType<GroupBoxElement>().Single();
        ComparisonCondition condition = Assert.IsType<ComparisonCondition>(group.VisibleIf);
        Assert.Equal("mode", condition.Key);
        Assert.Equal(ComparisonOperator.Equals, condition.Operator);

        InputElement summary = restored.Inputs().Single(input => input.Key == "summary");
        Assert.IsType<FormatComputed>(summary.Computed);

        InputElement height = restored.Inputs().Single(input => input.Key == "height");
        Assert.IsType<RangeRule>(Assert.Single(height.Rules));
    }

    /// <summary>The real test of a round trip: the restored form behaves identically.</summary>
    [Fact]
    public void A_restored_form_behaves_the_same_as_the_original()
    {
        FormSession original = new(RichForm());
        FormSession restored = new(FormJson.Deserialize(FormJson.Serialize(RichForm())));

        original.SetValue("mode", "manual");
        restored.SetValue("mode", "manual");

        Assert.Equal(
            original.GetState("verbose")!.IsVisible,
            restored.GetState("verbose")!.IsVisible);

        original.SetValue("name", "Grace");
        restored.SetValue("name", "Grace");

        Assert.Equal(original.GetValue("summary"), restored.GetValue("summary"));
    }

    [Fact]
    public void Enums_are_written_as_names_so_the_file_reads_like_a_form()
    {
        string json = FormJson.Serialize(RichForm());

        Assert.Contains("\"dark\"", json, StringComparison.Ordinal);
        Assert.Contains("\"equals\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"mode\": 2", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Every_element_carries_a_type_discriminator()
    {
        string json = FormJson.Serialize(RichForm());

        Assert.Contains("\"$type\": \"textBox\"", json, StringComparison.Ordinal);
        Assert.Contains("\"$type\": \"groupBox\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void A_document_from_a_newer_schema_is_refused_with_an_explanation()
    {
        string json = FormJson.Serialize(RichForm())
            .Replace("\"schemaVersion\": 1", "\"schemaVersion\": 99", StringComparison.Ordinal);

        InterludeJsonException error = Assert.Throws<InterludeJsonException>(() => FormJson.Deserialize(json));

        Assert.Contains("99", error.Message, StringComparison.Ordinal);
        Assert.Contains("Update Interlude", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Malformed_json_produces_a_readable_error()
    {
        InterludeJsonException error = Assert.Throws<InterludeJsonException>(
            () => FormJson.Deserialize("{ not json"));

        Assert.Contains("not valid JSON", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Serializing must not depend on the machine's locale. On a German machine an unguarded
    /// implementation writes 1,75 and produces a file no other machine can read.
    /// </summary>
    [Fact]
    public void Serialization_is_culture_invariant()
    {
        CultureInfo original = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
            string german = FormJson.Serialize(RichForm());

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            string invariant = FormJson.Serialize(RichForm());

            Assert.Equal(invariant, german);
            Assert.Contains("1.75", german, StringComparison.Ordinal);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = original;
        }
    }

    [Fact]
    public void Values_round_trip_for_pre_filling_a_form()
    {
        Dictionary<string, object?> values = new()
        {
            ["name"] = "Ada",
            ["count"] = 3,
            ["ratio"] = 0.5d,
            ["flag"] = true,
            ["picked"] = new List<object?> { "a", "b" },
            ["when"] = new DateTime(2026, 1, 2, 3, 4, 5),
        };

        Dictionary<string, object?> restored = FormJson.DeserializeValues(FormJson.SerializeValues(values));

        Assert.Equal("Ada", restored["name"]);
        Assert.Equal(3, restored["count"]);
        Assert.Equal(0.5d, restored["ratio"]);
        Assert.Equal(true, restored["flag"]);
        Assert.Equal(new DateTime(2026, 1, 2, 3, 4, 5), restored["when"]);
        Assert.Equal(new object?[] { "a", "b" }, Assert.IsType<List<object?>>(restored["picked"]));
    }

    /// <summary>
    /// Objects JSON cannot carry degrade to their text rather than failing the save, and the
    /// lossiness is the documented behaviour rather than a surprise.
    /// </summary>
    [Fact]
    public void An_option_value_JSON_cannot_represent_degrades_to_its_text()
    {
        FormDefinition form = new()
        {
            Title = "Opaque",
            Elements = new FormElement[]
            {
                new DropdownElement
                {
                    Key = "thing",
                    Options = new[] { new OptionItem { Value = new Uri("https://example.com/a"), Display = "A" } },
                },
            },
        };

        FormDefinition restored = FormJson.Deserialize(FormJson.Serialize(form));
        DropdownElement dropdown = restored.AllElements().OfType<DropdownElement>().Single();

        Assert.Equal("https://example.com/a", dropdown.Options[0].Value);
    }

    [Fact]
    public void An_empty_form_round_trips()
    {
        FormDefinition restored = FormJson.Deserialize(FormJson.Serialize(new FormDefinition { Title = "Empty" }));

        Assert.Equal("Empty", restored.Title);
        Assert.Empty(restored.Elements);
    }
}
