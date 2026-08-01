using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Interlude.Model;
using Interlude.Theming;
using Interlude.Rendering.Wpf;
using Xunit;

namespace Interlude.Tests;

/// <summary>
/// Guards the seam between the XAML and the theme applier.
///
/// The two halves of the theming system are joined by nothing but a string. A control template
/// asks for <c>{DynamicResource Interlude.BorderThickness}</c>; the applier writes a key spelled
/// the same way; nothing checks that they agree. When they do not, WPF does not complain — an
/// unresolved dynamic lookup leaves the property at its default and the form simply renders with
/// hairline borders and no shadow, looking merely a bit plain rather than broken. That is the
/// worst kind of bug to have: silent, cosmetic, and invisible to every other test in this suite.
/// </summary>
public class ThemeResourceTests
{
    /// <summary>
    /// Keys the templates reference but the applier deliberately leaves unwritten, with the reason.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> IntentionallyAbsent =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ThemeKeys.ControlShadow] =
                "written only when the theme offsets shadows; unresolved is how 'no shadow' is expressed",
        };

    private static IReadOnlyList<string> ReferencedKeys { get; } = ScanThemeXaml();

    [Fact]
    public void The_themes_reference_at_least_the_keys_we_expect()
    {
        // A sanity check on the scanner itself: if the regex or the paths ever stop matching, the
        // tests below would pass vacuously over an empty list.
        Assert.True(ReferencedKeys.Count > 20,
            $"Only found {ReferencedKeys.Count} dynamic resource references in the theme XAML. " +
            "The scanner is probably looking in the wrong place.");
    }

    [Fact]
    public void Every_key_the_XAML_asks_for_is_declared_in_ThemeKeys()
    {
        HashSet<string> declared = typeof(ThemeKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToHashSet(StringComparer.Ordinal);

        string[] undeclared = ReferencedKeys.Where(key => !declared.Contains(key)).ToArray();

        Assert.True(undeclared.Length == 0,
            "The theme XAML asks for resource keys that ThemeKeys does not declare, so nothing " +
            "writes them and they will silently resolve to nothing: " + string.Join(", ", undeclared));
    }

    [WpfFact]
    public void Every_key_the_XAML_asks_for_is_written_by_the_applier()
    {
        WpfTestContext.EnsureApplication();

        Window scope = new();
        try
        {
            // The default theme exercises every branch: it has an outline, a shadow and heavy text.
            WpfThemeApplier.Apply(scope, ThemeDefinition.Default);

            string[] missing = ReferencedKeys
                .Where(key => !IntentionallyAbsent.ContainsKey(key))
                .Where(key => !scope.Resources.Contains(key))
                .ToArray();

            Assert.True(missing.Length == 0,
                "The theme XAML asks for keys the applier never writes: " + string.Join(", ", missing));
        }
        finally
        {
            scope.Close();
        }
    }

    /// <summary>
    /// The shadow key is absent rather than empty when a theme has no shadow, and that is load
    /// bearing: an unresolved lookup leaves <c>Effect</c> null, so no control pays for a render
    /// layer it cannot see. A future change that writes a transparent effect instead would cost
    /// every control in every form for nothing, and no visual test would notice.
    /// </summary>
    [WpfFact]
    public void A_theme_without_a_shadow_offset_writes_no_shadow_at_all()
    {
        WpfTestContext.EnsureApplication();

        Window scope = new();
        try
        {
            WpfThemeApplier.Apply(scope, new ThemeDefinition { Mode = AppearanceMode.Light });
            Assert.False(scope.Resources.Contains(ThemeKeys.ControlShadow));

            // ...and applying a shadowed theme to the same scope must bring it back, because a
            // form can be rethemed in place.
            WpfThemeApplier.Apply(scope, ThemeDefinition.Default);
            Assert.True(scope.Resources.Contains(ThemeKeys.ControlShadow));

            WpfThemeApplier.Apply(scope, new ThemeDefinition { Mode = AppearanceMode.Light });
            Assert.False(scope.Resources.Contains(ThemeKeys.ControlShadow));
        }
        finally
        {
            scope.Close();
        }
    }

    /// <summary>
    /// The date field is retemplated, and a retemplated <see cref="DatePicker"/> that misnames one
    /// of its parts loses its drop-down without any error: the calendar simply never appears.
    /// </summary>
    [WpfFact]
    public void The_retemplated_date_field_still_finds_its_parts()
    {
        WpfTestContext.EnsureApplication();

        FormDefinition form = new FormDefinition
        {
            Title = "Date",
            Elements = new FormElement[] { new DatePickerElement { Key = "when", Label = "When" } },
        }.WithResolvedKeys();

        Runtime.FormSession session = new(form);
        FormWindow window = new(form, session, ControlRendererRegistry.CreateDefault());

        try
        {
            window.Show();

            DatePicker picker = Descendants(window).OfType<DatePicker>().Single();
            picker.ApplyTemplate();

            Assert.NotNull(picker.Template.FindName("PART_TextBox", picker));
            Assert.NotNull(picker.Template.FindName("PART_Button", picker));
            Assert.NotNull(picker.Template.FindName("PART_Popup", picker));

            // The real proof: the control agrees to open, which it only does once its own
            // OnApplyTemplate has found the popup and put a calendar inside it.
            picker.IsDropDownOpen = true;
            Assert.True(picker.IsDropDownOpen);

            picker.IsDropDownOpen = false;
        }
        finally
        {
            window.Close();
        }
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);

        for (int i = 0; i < count; i++)
        {
            DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            yield return child;

            foreach (DependencyObject descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// Reads the checked-in theme XAML rather than the compiled resources: BAML has already
    /// resolved nothing and would need a parser of its own, and the source is what a contributor
    /// edits.
    /// </summary>
    private static IReadOnlyList<string> ScanThemeXaml()
    {
        string themes = Path.Combine(RepoPaths.SourceRoot, "Themes");
        Regex reference = new(@"DynamicResource\s+(Interlude\.[A-Za-z]+)", RegexOptions.Compiled);

        SortedSet<string> keys = new(StringComparer.Ordinal);

        foreach (string file in Directory.GetFiles(themes, "*.xaml"))
        {
            foreach (Match match in reference.Matches(File.ReadAllText(file)))
            {
                keys.Add(match.Groups[1].Value);
            }
        }

        return keys.ToArray();
    }
}
