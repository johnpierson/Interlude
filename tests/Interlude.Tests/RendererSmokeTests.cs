using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Interlude.Conditions;
using Interlude.Model;
using Interlude.Rendering.Wpf;
using Interlude.Runtime;
using Interlude.Theming;
using Xunit;

namespace Interlude.Tests;

/// <summary>
/// Smoke tests for the WPF layer: build the window, check a control exists for every element,
/// drive values through the session and read them back off the controls.
///
/// Deliberately not pixel tests. What matters is that the tree is built, the theme resolves,
/// state reaches the controls and edits reach the session — not that a border is two pixels wide.
/// </summary>
public class RendererSmokeTests
{
    /// <summary>A form using every element type the package ships.</summary>
    private static FormDefinition FullCatalogue() => new FormDefinition
    {
        Title = "Catalogue",
        Description = "Every control at once.",
        Elements = new FormElement[]
        {
            new TextBoxElement { Key = "text", Label = "Text", Placeholder = "type here" },
            new TextBoxElement { Key = "notes", Label = "Notes", IsMultiline = true, Lines = 3 },
            new PasswordElement { Key = "secret", Label = "Secret" },
            new NumericElement { Key = "number", Label = "Number", Unit = "mm" },
            new IntegerElement { Key = "count", Label = "Count" },
            new SliderElement { Key = "ratio", Label = "Ratio", Minimum = 0, Maximum = 10 },
            new DropdownElement { Key = "mode", Label = "Mode", Options = TestForms.Options("a", "b") },
            new RadioGroupElement { Key = "pick", Label = "Pick", Options = TestForms.Options("x", "y") },
            new CheckBoxElement { Key = "flag", Content = "Enabled" },
            new ToggleElement { Key = "toggle", Label = "Toggle", OnText = "On", OffText = "Off" },
            new ListSelectionElement { Key = "many", Label = "Many", Options = TestForms.Options("p", "q", "r") },
            new ListSelectionElement { Key = "one", Label = "One", AllowMultiple = false, Options = TestForms.Options("p", "q") },
            new TreeSelectionElement
            {
                Key = "tree",
                Label = "Tree",
                Roots = new[]
                {
                    new TreeNode
                    {
                        Display = "Root",
                        Value = "root",
                        Children = new[] { new TreeNode { Display = "Leaf", Value = "leaf" } },
                    },
                },
            },
            new DatePickerElement { Key = "date", Label = "Date" },
            new ColorPickerElement { Key = "colour", Label = "Colour", Presets = new[] { RgbColor.White } },
            new FilePickerElement { Key = "file", Label = "File" },
            new FolderPickerElement { Key = "folder", Label = "Folder" },
            new ModelSelectionElement { Key = "elements", Label = "Elements" },

            new LabelElement { Text = "A heading", HeadingLevel = 2 },
            new MarkdownElement { Text = "# Title\n\nSome **bold**, some *italic*, `code`, and a [link](https://example.com).\n\n- one\n- two\n\n---" },
            new ImageElement { Path = "does-not-exist.png", AlternateText = "missing" },
            new SeparatorElement { Caption = "Section" },
            new SpacerElement(),
            new ProgressElement { Value = 40 },
            new ButtonElement { Text = "Do a thing", Tag = "thing" },

            new VStackElement { Children = new FormElement[] { new LabelElement { Text = "in a stack" } } },
            new HStackElement { Children = new FormElement[] { new LabelElement { Text = "left" }, new LabelElement { Text = "right" } } },
            new GridElement
            {
                Columns = new[] { GridTrack.Auto, GridTrack.Star },
                Children = new FormElement[] { new LabelElement { Text = "cell" }, new LabelElement { Text = "cell" } },
            },
            new GroupBoxElement { Header = "Group", Children = new FormElement[] { new LabelElement { Text = "grouped" } } },
            new TabsElement
            {
                Children = new FormElement[]
                {
                    new TabPageElement { Header = "One", Children = new FormElement[] { new LabelElement { Text = "tab one" } } },
                    new TabPageElement { Header = "Two", Children = new FormElement[] { new LabelElement { Text = "tab two" } } },
                },
            },
            new ExpanderElement { Header = "More", Children = new FormElement[] { new LabelElement { Text = "expanded" } } },
            new CardElement { Header = "Card", Subheader = "with a subheader", Children = new FormElement[] { new LabelElement { Text = "carded" } } },
            new ScrollElement { Children = new FormElement[] { new LabelElement { Text = "scrolled" } } },
            new DockElement { Children = new FormElement[] { new LabelElement { Text = "docked" } } },
            new SplitViewElement { Children = new FormElement[] { new LabelElement { Text = "left" }, new LabelElement { Text = "right" } } },
        },
    }.WithResolvedKeys();

    [WpfFact]
    public void Every_element_in_the_catalogue_gets_exactly_one_control()
    {
        WpfTestContext.EnsureApplication();

        FormDefinition form = FullCatalogue();
        FormSession session = new(form);
        FormWindow window = new(form, session, ControlRendererRegistry.CreateDefault());

        try
        {
            IReadOnlyDictionary<FormElement, ElementView> views = ViewsOf(window);

            foreach (FormElement element in form.AllElements())
            {
                Assert.True(views.ContainsKey(element),
                    $"No control was built for {element.GetType().Name} (key '{element.Key}').");
            }
        }
        finally
        {
            window.Close();
        }
    }

    [WpfFact]
    public void No_element_falls_through_to_the_placeholder_renderer()
    {
        ControlRendererRegistry registry = ControlRendererRegistry.CreateDefault();

        foreach (FormElement element in FullCatalogue().AllElements())
        {
            Assert.True(registry.CanRender(element),
                $"{element.GetType().Name} has no registered renderer and would render as a placeholder.");
        }
    }

    /// <summary>
    /// A form containing one control this build has never heard of must still be usable. The
    /// alternative — throwing — turns "this graph needs a newer Interlude" into "this graph is
    /// broken", and takes the other nineteen working fields down with it.
    /// </summary>
    [WpfFact]
    public void An_unknown_element_renders_as_a_placeholder_rather_than_throwing()
    {
        WpfTestContext.EnsureApplication();

        UnknownElement unknown = new() { Label = "From the future" };
        FormDefinition form = new FormDefinition
        {
            Title = "Unknown",
            Elements = new FormElement[] { unknown },
        }.WithResolvedKeys();

        ControlRendererRegistry registry = ControlRendererRegistry.CreateDefault();
        Assert.False(registry.CanRender(unknown));

        FormSession session = new(form);
        FormWindow window = new(form, session, registry);

        try
        {
            ElementView view = ViewsOf(window).Values.Single();

            Assert.NotNull(view.Control);
            Assert.Contains("UnknownElement", DescendantText(view.Control), StringComparison.Ordinal);
        }
        finally
        {
            window.Close();
        }
    }

    [WpfFact]
    public void Values_written_by_the_session_reach_the_controls()
    {
        WpfTestContext.EnsureApplication();

        FormDefinition form = FullCatalogue();
        FormSession session = new(form);
        FormWindow window = new(form, session, ControlRendererRegistry.CreateDefault());

        try
        {
            session.SetValue("text", "hello");
            session.SetValue("number", 12.5d);
            session.SetValue("flag", true);
            session.SetValue("mode", "b");

            IReadOnlyDictionary<FormElement, ElementView> views = ViewsOf(window);

            Assert.Equal("hello", ReadByKey(views, "text"));
            Assert.Equal(12.5d, ReadByKey(views, "number"));
            Assert.Equal(true, ReadByKey(views, "flag"));
            Assert.Equal("b", ReadByKey(views, "mode"));
        }
        finally
        {
            window.Close();
        }
    }

    [WpfFact]
    public void Hiding_a_field_collapses_its_label_and_control_together()
    {
        WpfTestContext.EnsureApplication();

        FormElement reason = new TextBoxElement
        {
            Key = "reason",
            Label = "Reason",
            VisibleIf = new ComparisonCondition { Key = "flag", Operator = ComparisonOperator.IsChecked },
        };

        FormDefinition form = TestForms.Form(new CheckBoxElement { Key = "flag", Content = "Show" }, reason);
        FormSession session = new(form);
        FormWindow window = new(form, session, ControlRendererRegistry.CreateDefault());

        try
        {
            ElementView view = ViewsOf(window)[form.AllElements().Single(e => e.Key == "reason")];

            Assert.Equal(Visibility.Collapsed, view.Root.Visibility);

            session.SetValue("flag", true);
            Assert.Equal(Visibility.Visible, view.Root.Visibility);
        }
        finally
        {
            window.Close();
        }
    }

    [WpfFact]
    public void A_failing_field_is_flagged_for_the_theme_to_style()
    {
        WpfTestContext.EnsureApplication();

        FormElement name = new TextBoxElement
        {
            Key = "name",
            Label = "Name",
            RequiredIf = ConstantCondition.True,
        };

        FormDefinition form = TestForms.Form(name);
        FormSession session = new(form);
        FormWindow window = new(form, session, ControlRendererRegistry.CreateDefault());

        try
        {
            ElementView view = ViewsOf(window)[form.AllElements().Single(e => e.Key == "name")];

            Assert.False(FieldState.GetHasError(view.Control));

            session.Validate();

            Assert.True(FieldState.GetHasError(view.Control));
        }
        finally
        {
            window.Close();
        }
    }

    [WpfFact]
    public void The_theme_resolves_every_key_the_styles_ask_for()
    {
        WpfTestContext.EnsureApplication();

        Window window = new();
        try
        {
            WpfThemeApplier.Apply(window, ThemeDefinition.Default);

            foreach (string key in ThemeKeyNames())
            {
                Assert.True(window.Resources.Contains(key), $"The theme did not define '{key}'.");
            }

            Assert.IsType<SolidColorBrush>(window.Resources[ThemeKeys.Accent]);
            Assert.NotEmpty(window.Resources.MergedDictionaries);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// The hard rule: theming a form must not touch the host application's resources. Revit and
    /// Dynamo own that dictionary, and writing to it would restyle their UI.
    /// </summary>
    [WpfFact]
    public void Theming_a_form_never_writes_to_the_host_application_resources()
    {
        WpfTestContext.EnsureApplication();

        int before = Application.Current!.Resources.Count;
        int mergedBefore = Application.Current.Resources.MergedDictionaries.Count;

        FormDefinition form = FullCatalogue();
        FormSession session = new(form);
        FormWindow window = new(form, session, ControlRendererRegistry.CreateDefault());

        try
        {
            Assert.Equal(before, Application.Current.Resources.Count);
            Assert.Equal(mergedBefore, Application.Current.Resources.MergedDictionaries.Count);
            Assert.NotEmpty(window.Resources.MergedDictionaries);
        }
        finally
        {
            window.Close();
        }
    }

    [WpfFact]
    public void A_dark_theme_produces_a_different_palette_than_a_light_one()
    {
        WpfTestContext.EnsureApplication();

        Window light = new();
        Window dark = new();

        try
        {
            WpfThemeApplier.Apply(light, new ThemeDefinition { Mode = AppearanceMode.Light });
            WpfThemeApplier.Apply(dark, new ThemeDefinition { Mode = AppearanceMode.Dark });

            SolidColorBrush lightBackground = (SolidColorBrush)light.Resources[ThemeKeys.Background];
            SolidColorBrush darkBackground = (SolidColorBrush)dark.Resources[ThemeKeys.Background];

            Assert.NotEqual(lightBackground.Color, darkBackground.Color);
        }
        finally
        {
            light.Close();
            dark.Close();
        }
    }

    [WpfFact]
    public void A_custom_accent_is_applied_and_keeps_its_text_readable()
    {
        WpfTestContext.EnsureApplication();

        Window window = new();
        try
        {
            WpfThemeApplier.Apply(window, new ThemeDefinition
            {
                Mode = AppearanceMode.Light,
                Accent = RgbColor.Parse("#FFDD00"),
            });

            SolidColorBrush accent = (SolidColorBrush)window.Resources[ThemeKeys.Accent];
            SolidColorBrush onAccent = (SolidColorBrush)window.Resources[ThemeKeys.AccentForeground];

            Assert.Equal(Color.FromRgb(0xFF, 0xDD, 0x00), accent.Color);

            // A bright accent must take dark text, not white.
            Assert.True(onAccent.Color.R < 128);
        }
        finally
        {
            window.Close();
        }
    }

    private static IReadOnlyDictionary<FormElement, ElementView> ViewsOf(FormWindow window)
        => window.Context.Views;

    /// <summary>Concatenates the text of every TextBlock under an element.</summary>
    private static string DescendantText(DependencyObject root)
    {
        System.Text.StringBuilder text = new();

        void Walk(DependencyObject node)
        {
            if (node is TextBlock block)
            {
                text.Append(block.Text).Append(' ');
            }

            int count = VisualTreeHelper.GetChildrenCount(node);
            for (int i = 0; i < count; i++)
            {
                Walk(VisualTreeHelper.GetChild(node, i));
            }
        }

        Walk(root);
        return text.ToString();
    }

    private static object? ReadByKey(IReadOnlyDictionary<FormElement, ElementView> views, string key)
    {
        ElementView view = views.First(pair => pair.Key.Key == key).Value;
        return view.Renderer.ReadValue(view.Control);
    }

    private static IEnumerable<string> ThemeKeyNames()
        => typeof(ThemeKeys)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!);

    /// <summary>Stands in for a control type a future release might add.</summary>
    private sealed record UnknownElement : InputElement
    {
        public override object? GetFallbackValue() => null;
    }
}
