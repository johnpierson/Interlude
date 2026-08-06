using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Interlude.Conditions;
using Interlude.Model;
using Interlude.Runtime;
using Interlude.Serialization;
using Xunit;

namespace Interlude.Tests;

/// <summary>
/// How a computed value reads on screen.
///
/// Two separate jobs meet here. <see cref="ValueOps.ToDisplayString"/> decides how many digits of
/// a double are worth showing, and the format specifier in a template — <c>{total:0.00}</c> —
/// lets the author override that per placeholder. Both are invariant: they choose digits, not
/// punctuation.
/// </summary>
public class FormatTests
{
    /// <summary>Reads a template against a fixed set of answers, with no window involved.</summary>
    private static string Render(string template, params (string Key, object? Value)[] values)
    {
        Dictionary<string, object?> state = new(StringComparer.Ordinal);
        foreach ((string key, object? value) in values)
        {
            state[key] = value;
        }

        return (string)new FormatComputed { Template = template }.Compute(new DictionaryState(state))!;
    }

    private sealed class DictionaryState : IFormStateReader
    {
        private readonly Dictionary<string, object?> _values;

        internal DictionaryState(Dictionary<string, object?> values) => _values = values;

        public IReadOnlyCollection<string> Keys => _values.Keys;

        public object? GetValue(string key)
            => _values.TryGetValue(key, out object? value) ? value : null;

        public bool TryGetValue(string key, out object? value)
            => _values.TryGetValue(key, out value);
    }

    /// <summary>
    /// The complaint this whole change exists for: a total that landed on a representable-but-ugly
    /// double should read as the number the user was thinking of.
    /// </summary>
    [Fact]
    public void A_double_shows_the_number_a_person_would_write_rather_than_its_exact_form()
    {
        Assert.Equal("0.3", ValueOps.ToDisplayString(0.1d + 0.2d));
        Assert.Equal("546", ValueOps.ToDisplayString(12d * 45.5d));
        Assert.Equal("1.5", ValueOps.ToDisplayString(1.5d));
        Assert.Equal("0.333333333333333", ValueOps.ToDisplayString(1d / 3d));

        // And the exact form is still available, because matching and persistence need it.
        Assert.Equal("0.30000000000000004", ValueOps.ToStringInvariant(0.1d + 0.2d));
    }

    [Fact]
    public void Display_rendering_leaves_every_other_kind_of_value_alone()
    {
        Assert.Equal("Ada", ValueOps.ToDisplayString("Ada"));
        Assert.Equal("true", ValueOps.ToDisplayString(true));
        Assert.Equal(string.Empty, ValueOps.ToDisplayString(null));
        Assert.Equal("1, 2.5, 3", ValueOps.ToDisplayString(new object?[] { 1, 2.5d, 3 }));
    }

    [Fact]
    public void A_template_without_a_specifier_uses_the_display_form()
    {
        Assert.Equal(
            "Total: 0.3",
            Render("Total: {total}", ("total", 0.1d + 0.2d)));
    }

    [Fact]
    public void A_specifier_after_a_colon_says_how_the_value_should_look()
    {
        Assert.Equal("£5.50", Render("£{price:0.00}", ("price", 5.5d)));
        Assert.Equal("1,234.56", Render("{price:#,0.00}", ("price", 1234.56d)));
        Assert.Equal("042", Render("{code:000}", ("code", 42d)));
        Assert.Equal("25%", Render("{rate:0%}", ("rate", 0.25d)));
    }

    /// <summary>
    /// The reason the split is at the first colon and not the last: a time format contains one.
    /// </summary>
    [Fact]
    public void A_specifier_may_contain_its_own_colons()
    {
        DateTime when = new(2026, 8, 5, 14, 30, 0);

        Assert.Equal("14:30", Render("{when:HH:mm}", ("when", when)));
        Assert.Equal("2026-08-05", Render("{when:yyyy-MM-dd}", ("when", when)));
    }

    /// <summary>
    /// Form values are loosely typed — the same answer may be a double from a spinner or a string
    /// from JSON — and a specifier that worked yesterday should not stop working because of it.
    /// </summary>
    [Fact]
    public void A_number_that_arrived_as_text_still_formats()
    {
        Assert.Equal("5.50", Render("{price:0.00}", ("price", "5.5")));
    }

    /// <summary>
    /// Templates are re-rendered on every keystroke, so a specifier that is wrong — or merely
    /// half-typed — has to degrade rather than throw. Same rule as the unterminated placeholder.
    ///
    /// "Q" is the case that actually throws: a lone character that is not one of .NET's standard
    /// specifiers. Longer nonsense does not, per the note below.
    /// </summary>
    [Fact]
    public void An_unusable_specifier_falls_back_to_the_plain_value()
    {
        Assert.Equal("5.5", Render("{price:Q}", ("price", 5.5d)));
        Assert.Equal("Ada", Render("{name:0.00}", ("name", "Ada")));
        Assert.Equal(string.Empty, Render("{missing:0.00}"));
    }

    /// <summary>
    /// Characters .NET does not recognise inside a longer specifier are literals, not errors —
    /// which is how a unit gets carried along with the number it belongs to.
    /// </summary>
    [Fact]
    public void A_specifier_may_carry_literal_text_along_with_the_number()
    {
        Assert.Equal("5.50 kg", Render("{weight:0.00 kg}", ("weight", 5.5d)));
    }

    [Fact]
    public void A_multi_select_of_numbers_formats_item_by_item()
    {
        Assert.Equal(
            "1.50, 2.25",
            Render("{prices:0.00}", ("prices", new object?[] { 1.5d, 2.25d })));
    }

    /// <summary>
    /// The specifier is not part of the key. If it were, the dependency graph would never fire and
    /// the field would sit at its first value.
    /// </summary>
    [Fact]
    public void A_placeholder_depends_on_its_key_and_not_on_its_specifier()
    {
        FormatComputed format = new() { Template = "{quantity} at £{price:0.00} on {when:HH:mm}" };

        Assert.Equal(
            new[] { "quantity", "price", "when" },
            format.DependsOn());
    }

    /// <summary>End to end: a formatted summary recomputes as the fields behind it change.</summary>
    [Fact]
    public void A_formatted_summary_updates_with_the_fields_it_reads()
    {
        InputElement summary = TestForms.Text("summary") with
        {
            Computed = new FormatComputed { Template = "{quantity} at £{price:0.00}" },
        };

        FormSession session = new(TestForms.Form(
            TestForms.Number("quantity", 3d),
            TestForms.Number("price", 45.5d),
            summary));

        Assert.Equal("3 at £45.50", session.GetValue("summary"));

        session.SetValue("price", 1234.5d);
        Assert.Equal("3 at £1234.50", session.GetValue("summary"));
    }

    /// <summary>
    /// Specifiers choose digits, not punctuation. A form authored in London and opened in Berlin
    /// must show the same text, which is the same rule the rest of the engine follows.
    /// </summary>
    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    public void Formatted_output_does_not_follow_the_machine_locale(string culture)
    {
        CultureInfo original = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);

            Assert.Equal("1,234.56", Render("{price:#,0.00}", ("price", 1234.56d)));
            Assert.Equal("0.3", ValueOps.ToDisplayString(0.1d + 0.2d));
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = original;
        }
    }

    /// <summary>
    /// The shipped example, end to end.
    ///
    /// Its VAT chain lands on exactly 655.2, so this is the padding half of the argument rather
    /// than the rounding half: money wants both decimal places whatever the arithmetic produced,
    /// and only the specifier gets them. Worth pinning, because it is the file a reader opens.
    /// </summary>
    [Fact]
    public void The_computed_values_sample_reads_the_way_its_author_meant()
    {
        FormDefinition form = FormJson.Load(
            Path.Combine(RepoPaths.Root, "samples", "computed-values.json"));

        FormSession session = new(form);

        Assert.Equal("12 items for Ada, £655.20 including VAT", session.GetValue("summary"));

        // Display changed; the answer did not. The stored total is the double it always was,
        // which is what the graph downstream receives and what the saved file records.
        Assert.Equal(655.2d, session.GetValue("total"));
    }

    /// <summary>
    /// The compatibility claim in one test. A colon in a placeholder used to read a key that could
    /// not exist and rendered as nothing, so no working template can change meaning — and doubling
    /// braces still escapes them.
    /// </summary>
    [Fact]
    public void Templates_that_worked_before_still_mean_what_they_did()
    {
        Assert.Equal("Hello Ada", Render("Hello {name}", ("name", "Ada")));
        Assert.Equal("{name}", Render("{{name}}", ("name", "Ada")));
        Assert.Equal("100%", Render("100%", ("name", "Ada")));
        Assert.Equal("half {typed", Render("half {typed", ("name", "Ada")));
    }
}
