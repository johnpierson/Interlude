using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Autodesk.DesignScript.Runtime;
using Interlude.Model;
using Interlude.Theming;

namespace Interlude.Rendering.Wpf.Controls;

/// <summary>
/// A colour field: a swatch that opens a small picker, beside a hex box for people who already
/// know the value they want.
///
/// The picker is channel sliders plus preset swatches rather than a colour wheel. A form asking
/// for a view filter colour or a branding accent is answered from a palette or from a hex code
/// far more often than by hunting around a gradient, and sliders cost no dependencies.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class ColorField : Grid
{
    private readonly ColorPickerElement _element;
    private readonly Border _swatch;
    private readonly TextBox _hex;
    private readonly Popup _popup;
    private readonly Slider _red;
    private readonly Slider _green;
    private readonly Slider _blue;
    private readonly Slider? _alpha;

    private RgbColor _value = RgbColor.Black;
    private bool _isWriting;

    internal ColorField(ColorPickerElement element)
    {
        _element = element;

        ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        ToggleButton opener = new()
        {
            Width = 44,
            Padding = new Thickness(4),
            Margin = new Thickness(0, 0, 6, 0),
        };
        opener.SetResourceReference(StyleProperty, "Interlude.SwatchButton");

        _swatch = new Border { CornerRadius = new CornerRadius(2), MinHeight = 14 };
        _swatch.SetResourceReference(Border.BorderBrushProperty, ThemeKeys.Border);
        _swatch.SetResourceReference(Border.BorderThicknessProperty, ThemeKeys.BorderThickness);
        opener.Content = _swatch;

        SetColumn(opener, 0);
        Children.Add(opener);

        _hex = new TextBox { VerticalContentAlignment = VerticalAlignment.Center };
        _hex.LostFocus += OnHexCommitted;
        _hex.KeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                OnHexCommitted(_hex, EventArgs.Empty);
            }
        };
        SetColumn(_hex, 1);
        Children.Add(_hex);

        _red = BuildChannelSlider();
        _green = BuildChannelSlider();
        _blue = BuildChannelSlider();
        _alpha = element.ShowAlpha ? BuildChannelSlider() : null;

        _popup = new Popup
        {
            PlacementTarget = opener,
            Placement = PlacementMode.Bottom,
            StaysOpen = false,
            AllowsTransparency = true,
            Child = BuildPickerPanel(),
        };

        opener.Checked += (_, _) => _popup.IsOpen = true;
        opener.Unchecked += (_, _) => _popup.IsOpen = false;
        _popup.Closed += (_, _) => opener.IsChecked = false;

        Write(RgbColor.Black);
    }

    /// <summary>Raised when the user changes the colour.</summary>
    internal event EventHandler? ColorChanged;

    /// <summary>The current colour.</summary>
    internal RgbColor Value => _value;

    /// <summary>Sets the colour without raising <see cref="ColorChanged"/>.</summary>
    internal void Write(RgbColor color)
    {
        _isWriting = true;
        try
        {
            _value = _element.ShowAlpha ? color : color with { Alpha = 255 };

            _swatch.Background = _value.ToBrush();

            string hex = _value.ToHex();
            if (!string.Equals(_hex.Text, hex, StringComparison.OrdinalIgnoreCase))
            {
                _hex.Text = hex;
            }

            _red.Value = _value.Red;
            _green.Value = _value.Green;
            _blue.Value = _value.Blue;

            if (_alpha is not null)
            {
                _alpha.Value = _value.Alpha;
            }
        }
        finally
        {
            _isWriting = false;
        }
    }

    private FrameworkElement BuildPickerPanel()
    {
        StackPanel panel = new() { Margin = new Thickness(10), MinWidth = 220 };

        if (_element.Presets.Count > 0)
        {
            WrapPanel presets = new() { Margin = new Thickness(0, 0, 0, 8) };

            foreach (RgbColor preset in _element.Presets)
            {
                Button swatch = new()
                {
                    Width = 22,
                    Height = 22,
                    Margin = new Thickness(0, 0, 4, 4),
                    Background = preset.ToBrush(),
                    ToolTip = preset.ToHex(),
                };
                swatch.SetResourceReference(StyleProperty, "Interlude.PresetSwatch");

                RgbColor captured = preset;
                swatch.Click += (_, _) =>
                {
                    Write(captured);
                    ColorChanged?.Invoke(this, EventArgs.Empty);
                };

                presets.Children.Add(swatch);
            }

            panel.Children.Add(presets);
        }

        panel.Children.Add(BuildChannelRow("R", _red));
        panel.Children.Add(BuildChannelRow("G", _green));
        panel.Children.Add(BuildChannelRow("B", _blue));

        if (_alpha is not null)
        {
            panel.Children.Add(BuildChannelRow("A", _alpha));
        }

        Border frame = new()
        {
            Child = panel,
            CornerRadius = new CornerRadius(4),
        };
        frame.SetResourceReference(Border.BorderThicknessProperty, ThemeKeys.BorderThickness);
        frame.SetResourceReference(Border.BackgroundProperty, ThemeKeys.Surface);
        frame.SetResourceReference(Border.BorderBrushProperty, ThemeKeys.BorderStrong);
        return frame;
    }

    private Slider BuildChannelSlider()
    {
        Slider slider = new()
        {
            Minimum = 0,
            Maximum = 255,
            SmallChange = 1,
            LargeChange = 16,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        slider.ValueChanged += (_, _) =>
        {
            if (_isWriting)
            {
                return;
            }

            RgbColor next = new(
                (byte)_red.Value,
                (byte)_green.Value,
                (byte)_blue.Value,
                _alpha is null ? (byte)255 : (byte)_alpha.Value);

            Write(next);
            ColorChanged?.Invoke(this, EventArgs.Empty);
        };

        return slider;
    }

    private static FrameworkElement BuildChannelRow(string channel, Slider slider)
    {
        Grid row = new() { Margin = new Thickness(0, 2, 0, 2) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });

        TextBlock label = new() { Text = channel, VerticalAlignment = VerticalAlignment.Center };
        label.SetResourceReference(TextBlock.ForegroundProperty, ThemeKeys.ForegroundMuted);

        TextBlock readout = new()
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        readout.SetResourceReference(TextBlock.ForegroundProperty, ThemeKeys.ForegroundMuted);
        readout.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Value")
        {
            Source = slider,
            StringFormat = "0",
        });

        SetColumn(label, 0);
        SetColumn(slider, 1);
        SetColumn(readout, 2);

        row.Children.Add(label);
        row.Children.Add(slider);
        row.Children.Add(readout);
        return row;
    }

    private void OnHexCommitted(object? sender, EventArgs e)
    {
        if (_isWriting)
        {
            return;
        }

        if (RgbColor.TryParse(_hex.Text, out RgbColor parsed))
        {
            Write(parsed);
            ColorChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        // Unparseable text reverts rather than clearing the colour, so a typo costs a keystroke
        // instead of the answer.
        Write(_value);
    }
}
