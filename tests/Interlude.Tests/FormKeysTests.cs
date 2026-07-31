using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Interlude.Model;
using Xunit;

namespace Interlude.Tests;

/// <summary>
/// Key derivation is a published contract: graphs index results by these strings. These tests
/// are the contract, and changing one of them means bumping <see cref="FormKeys.SlugVersion"/>.
/// </summary>
public class FormKeysTests
{
    [Theory]
    [InlineData("Wall Type", "wall_type")]
    [InlineData("wallType", "walltype")]
    [InlineData("  Leading and trailing  ", "leading_and_trailing")]
    [InlineData("Height (mm)", "height_mm")]
    [InlineData("A---B", "a_b")]
    [InlineData("100%", "100")]
    [InlineData("!!!", "field")]
    [InlineData("", "field")]
    [InlineData(null, "field")]
    public void Slugify_follows_the_documented_rules(string? label, string expected)
        => Assert.Equal(expected, FormKeys.Slugify(label));

    [Fact]
    public void Slugify_strips_accents_rather_than_dropping_the_letter()
    {
        Assert.Equal("hohe", FormKeys.Slugify("Höhe"));
        Assert.Equal("elevation_finie", FormKeys.Slugify("Élévation finie"));
    }

    /// <summary>
    /// A German or Turkish machine must produce the same keys as an English one, or the same
    /// graph stops working when it travels. Turkish is the interesting case: its lowercase 'I'
    /// is a dotless 'ı', which would slug "ID" to something no ordinal lookup would ever match.
    /// </summary>
    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("tr-TR")]
    public void Slugify_is_culture_invariant(string culture)
    {
        CultureInfo original = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);

            Assert.Equal("wall_id", FormKeys.Slugify("Wall ID"));
            Assert.Equal("instance", FormKeys.Slugify("INSTANCE"));
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = original;
        }
    }

    [Fact]
    public void Duplicate_labels_get_numbered_suffixes_in_document_order()
    {
        FormDefinition form = TestForms.Form(
            new TextBoxElement { Label = "Name" },
            new TextBoxElement { Label = "Name" },
            new TextBoxElement { Label = "Name" });

        Assert.Equal(
            new[] { "name", "name_2", "name_3" },
            form.Inputs().Select(input => input.Key));
    }

    [Fact]
    public void Explicit_keys_are_honoured_and_never_rewritten()
    {
        FormDefinition form = TestForms.Form(
            new TextBoxElement { Key = "customKey", Label = "Something Else" },
            new TextBoxElement { Label = "Custom Key" });

        Assert.Equal(new[] { "customKey", "custom_key" }, form.Inputs().Select(input => input.Key));
    }

    [Fact]
    public void An_explicit_key_that_collides_with_a_derived_one_still_resolves()
    {
        FormDefinition form = TestForms.Form(
            new TextBoxElement { Label = "Name" },
            new TextBoxElement { Key = "name" });

        Assert.Equal(new[] { "name", "name_2" }, form.Inputs().Select(input => input.Key));
    }

    [Fact]
    public void Keys_are_assigned_through_nested_containers()
    {
        FormDefinition form = TestForms.Form(
            new VStackElement
            {
                Children = new FormElement[]
                {
                    new GroupBoxElement
                    {
                        Header = "Group",
                        Children = new FormElement[] { new TextBoxElement { Label = "Inner Field" } },
                    },
                },
            });

        Assert.Equal(new[] { "inner_field" }, form.Inputs().Select(input => input.Key));
    }

    [Fact]
    public void Assign_is_idempotent()
    {
        FormDefinition once = TestForms.Form(
            new TextBoxElement { Label = "Name" },
            new TextBoxElement { Label = "Name" });

        FormDefinition twice = once.WithResolvedKeys();

        Assert.Equal(
            once.Inputs().Select(input => input.Key),
            twice.Inputs().Select(input => input.Key));
    }

    [Fact]
    public void Elements_that_produce_no_value_are_left_keyless()
    {
        FormDefinition form = TestForms.Form(
            new LabelElement { Text = "Just a heading" },
            new SeparatorElement());

        Assert.All(form.AllElements(), element => Assert.Equal(string.Empty, element.Key));
    }

    [Fact]
    public void MakeUnique_records_every_key_it_hands_out()
    {
        HashSet<string> used = new();

        Assert.Equal("name", FormKeys.MakeUnique("name", used));
        Assert.Equal("name_2", FormKeys.MakeUnique("name", used));
        Assert.Equal("name_3", FormKeys.MakeUnique("name", used));
    }
}
