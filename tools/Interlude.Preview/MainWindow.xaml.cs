using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Interlude.Model;
using Interlude.Rendering;
using Interlude.Rendering.Wpf;
using Interlude.Runtime;
using Interlude.Serialization;
using Interlude.Theming;
using Microsoft.Win32;

namespace Interlude.Preview;

/// <summary>
/// The harness shell: pick a form, adjust the theme, show it, read the answers.
///
/// Hot reload watches the JSON file a form was loaded from and reshows it on save, which turns
/// "edit the form, rebuild, restart Revit" into "edit the file". That is the whole reason a form
/// is a document rather than a pile of code.
/// </summary>
public partial class MainWindow : Window
{
    private static readonly (string Name, string Hex)[] Accents =
    {
        ("Default", ""),
        ("Blue", "#2F6FEB"),
        ("Teal", "#0E7490"),
        ("Green", "#1A7F37"),
        ("Amber", "#B45309"),
        ("Rose", "#BE123C"),
        ("Violet", "#6E40C9"),
    };

    private FileSystemWatcher? _watcher;
    private string? _loadedPath;
    private FormDefinition? _loaded;
    private bool _isLoading = true;

    public MainWindow()
    {
        InitializeComponent();

        SampleList.ItemsSource = Gallery.Samples;
        ModeBox.ItemsSource = Enum.GetNames<AppearanceMode>();
        DensityBox.ItemsSource = Enum.GetNames<ThemeDensity>();
        AccentBox.ItemsSource = Accents.Select(accent => accent.Name).ToList();

        ModeBox.SelectedItem = nameof(AppearanceMode.Dark);
        DensityBox.SelectedItem = nameof(ThemeDensity.Comfortable);
        AccentBox.SelectedIndex = 0;
        SampleList.SelectedIndex = 0;

        _isLoading = false;
        Refresh();

        Closed += (_, _) => DisposeWatcher();
    }

    /// <summary>The form currently selected, whether from the gallery or from a file.</summary>
    private FormDefinition CurrentDefinition()
    {
        FormDefinition definition = _loaded
            ?? (SampleList.SelectedItem as Sample)?.Build()
            ?? Gallery.Samples[0].Build();

        return definition with { Theme = BuildTheme() };
    }

    private ThemeDefinition BuildTheme()
    {
        string accentHex = Accents
            .First(accent => accent.Name == (string)(AccentBox.SelectedItem ?? "Default"))
            .Hex;

        return new ThemeDefinition
        {
            Mode = Enum.TryParse((string?)ModeBox.SelectedItem, out AppearanceMode mode) ? mode : AppearanceMode.Dark,
            Density = Enum.TryParse((string?)DensityBox.SelectedItem, out ThemeDensity density) ? density : ThemeDensity.Comfortable,
            Accent = accentHex.Length == 0 ? null : RgbColor.Parse(accentHex),
            CornerRadius = RadiusSlider.Value,
            LabelWidth = LabelWidthSlider.Value,
            ReducedMotion = ReducedMotionBox.IsChecked == true,
        };
    }

    private void Refresh()
    {
        if (_isLoading)
        {
            return;
        }

        FormDefinition definition = CurrentDefinition();

        SummaryText.Text = _loadedPath is not null
            ? $"Loaded from {_loadedPath}"
            : (SampleList.SelectedItem as Sample)?.Summary ?? string.Empty;

        DefinitionText.Text = FormJson.Serialize(definition);

        // Reporting authoring problems here mirrors what the Form.Check node reports in a graph.
        FormSession probe;
        try
        {
            probe = new FormSession(definition);
        }
        catch (InterludeException error)
        {
            SummaryText.Text += "\n\n" + error.Message;
            return;
        }

        if (probe.Warnings.Count > 0)
        {
            SummaryText.Text += "\n\n" + string.Join("\n", probe.Warnings);
        }
    }

    private void OnShowForm(object sender, RoutedEventArgs e)
    {
        FormDefinition definition = CurrentDefinition();

        FormSession session;
        try
        {
            session = new FormSession(definition);
        }
        catch (InterludeException error)
        {
            MessageBox.Show(this, error.Message, "Interlude", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IFormRenderer renderer = new WpfFormRenderer();
        FormResult result = renderer.ShowModal(definition, session);

        ResultText.Text = Describe(result);
    }

    private static string Describe(FormResult result)
    {
        System.Text.StringBuilder text = new();
        text.AppendLine(CultureInfo.InvariantCulture, $"wasSubmitted : {result.WasSubmitted}");
        text.AppendLine(CultureInfo.InvariantCulture, $"buttonClicked: {result.ButtonClicked}");
        text.AppendLine();
        text.AppendLine("values:");
        text.AppendLine(FormJson.SerializeValues(result.Values));
        return text.ToString();
    }

    private void OnSampleChanged(object sender, SelectionChangedEventArgs e)
    {
        // Choosing a gallery sample abandons whatever file was loaded, watcher and all.
        _loaded = null;
        _loadedPath = null;
        DisposeWatcher();
        Refresh();
    }

    private void OnThemeChanged(object sender, RoutedEventArgs e) => Refresh();

    private void OnThemeChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => Refresh();

    private void OnOpenJson(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new()
        {
            Filter = "Interlude forms|*.json|All files|*.*",
            Title = "Open a form",
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        LoadFile(dialog.FileName);
    }

    private void LoadFile(string path)
    {
        try
        {
            _loaded = FormJson.Load(path);
            _loadedPath = path;
            WatchFile(path);
            Refresh();
        }
        catch (Exception error) when (error is InterludeException or IOException)
        {
            MessageBox.Show(this, error.Message, "Interlude", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnSaveJson(object sender, RoutedEventArgs e)
    {
        SaveFileDialog dialog = new()
        {
            Filter = "Interlude forms|*.json",
            DefaultExt = "json",
            FileName = (SampleList.SelectedItem as Sample)?.Name.Replace(' ', '-').ToLowerInvariant() + ".json",
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        FormJson.Save(CurrentDefinition(), dialog.FileName);
        LoadFile(dialog.FileName);
    }

    private void WatchFile(string path)
    {
        DisposeWatcher();

        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (directory is null)
        {
            return;
        }

        _watcher = new FileSystemWatcher(directory, Path.GetFileName(path))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true,
        };

        _watcher.Changed += OnFileChanged;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (HotReloadBox.IsChecked != true || _loadedPath is null)
        {
            return;
        }

        Dispatcher.InvokeAsync(() =>
        {
            try
            {
                // Editors write in stages, so the first notification often arrives while the file
                // is still locked or half-written. Retrying briefly is simpler and more reliable
                // than trying to guess when the editor has finished.
                for (int attempt = 0; attempt < 5; attempt++)
                {
                    try
                    {
                        _loaded = FormJson.Load(_loadedPath!);
                        Refresh();
                        OnShowForm(this, new RoutedEventArgs());
                        return;
                    }
                    catch (Exception error) when (error is IOException or InterludeException)
                    {
                        System.Threading.Thread.Sleep(80);
                    }
                }
            }
            catch (Exception error)
            {
                SummaryText.Text = "Could not reload: " + error.Message;
            }
        });
    }

    private void DisposeWatcher()
    {
        if (_watcher is null)
        {
            return;
        }

        _watcher.Changed -= OnFileChanged;
        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();
        _watcher = null;
    }
}
