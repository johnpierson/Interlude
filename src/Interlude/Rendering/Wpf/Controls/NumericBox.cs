using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Autodesk.DesignScript.Runtime;
using Interlude.Theming;

namespace Interlude.Rendering.Wpf.Controls;

/// <summary>
/// A number entry with optional spinner buttons and a unit suffix.
///
/// Derives from <see cref="Border"/> rather than <see cref="Grid"/> so the theme can give it the
/// same surface, corner radius and focus outline as a plain text box — a numeric field that does
/// not match the text field beside it is the fastest way to make a form look assembled from parts.
///
/// Culture is handled the way the rest of the package handles it, but inverted for the one place
/// it belongs: the *user's* culture. Someone on a German machine types "1,5" and means one and a
/// half, so display and parsing here follow <see cref="CultureInfo.CurrentCulture"/> — while the
/// value handed to the session is a <see cref="double"/>, which has no culture at all. Invariant
/// parsing is accepted as a fallback so a default supplied by a graph as "1.5" still loads.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class NumericBox : Border
{
    private readonly TextBox _entry;
    private readonly int _decimals;
    private readonly bool _isInteger;
    private readonly double? _minimum;
    private readonly double? _maximum;
    private readonly double _increment;

    private double _value;
    private bool _isWriting;

    internal NumericBox(
        double? minimum,
        double? maximum,
        double increment,
        int decimals,
        string? unit,
        bool showSpinner,
        bool isInteger)
    {
        _minimum = minimum;
        _maximum = maximum;
        _increment = increment <= 0d ? 1d : increment;
        _decimals = isInteger ? 0 : Math.Max(0, decimals);
        _isInteger = isInteger;

        Grid layout = new();
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _entry = new TextBox
        {
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            VerticalContentAlignment = VerticalAlignment.Center,
            Padding = new Thickness(0),
        };
        _entry.SetResourceReference(Control.ForegroundProperty, ThemeKeys.Foreground);
        _entry.TextChanged += OnTextChanged;
        _entry.LostFocus += OnLostFocus;
        _entry.PreviewKeyDown += OnPreviewKeyDown;

        Grid.SetColumn(_entry, 0);
        layout.Children.Add(_entry);

        if (!string.IsNullOrWhiteSpace(unit))
        {
            TextBlock suffix = new()
            {
                Text = unit,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 2, 0),
                IsHitTestVisible = false,
            };
            suffix.SetResourceReference(TextBlock.ForegroundProperty, ThemeKeys.ForegroundMuted);

            Grid.SetColumn(suffix, 1);
            layout.Children.Add(suffix);
        }

        if (showSpinner)
        {
            StackPanel spinner = BuildSpinner();
            Grid.SetColumn(spinner, 2);
            layout.Children.Add(spinner);
        }

        Child = layout;

        // Clicking anywhere in the field, including the padding, should start editing.
        MouseLeftButtonDown += (_, _) => _entry.Focus();
        Focusable = false;
    }

    /// <summary>Raised when the user changes the number, never when it is written programmatically.</summary>
    internal event EventHandler? ValueChanged;

    /// <summary>The current number.</summary>
    internal double Value
    {
        get => _value;
        set => Write(value);
    }

    /// <summary>The inner text box, for read-only state and focus management.</summary>
    internal TextBox Entry => _entry;

    /// <summary>Sets the number without raising <see cref="ValueChanged"/>.</summary>
    internal void Write(double value)
    {
        double clamped = Clamp(value);
        _value = clamped;

        _isWriting = true;
        try
        {
            string formatted = Format(clamped);

            // Only rewrite the text when it actually differs: assigning Text moves the caret to
            // the end, which is intolerable while someone is typing in the middle of a number.
            if (!string.Equals(_entry.Text, formatted, StringComparison.Ordinal))
            {
                _entry.Text = formatted;
            }
        }
        finally
        {
            _isWriting = false;
        }
    }

    private StackPanel BuildSpinner()
    {
        StackPanel spinner = new() { Orientation = Orientation.Vertical, Margin = new Thickness(2, 0, 0, 0) };

        RepeatButton up = new() { Content = "", Focusable = false };
        RepeatButton down = new() { Content = "", Focusable = false };

        up.SetResourceReference(FrameworkElement.StyleProperty, "Interlude.SpinnerButton");
        down.SetResourceReference(FrameworkElement.StyleProperty, "Interlude.SpinnerButton");

        up.Click += (_, _) => Step(_increment);
        down.Click += (_, _) => Step(-_increment);

        spinner.Children.Add(up);
        spinner.Children.Add(down);
        return spinner;
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Up)
        {
            Step(_increment);
            e.Handled = true;
        }
        else if (e.Key == Key.Down)
        {
            Step(-_increment);
            e.Handled = true;
        }
    }

    private void Step(double delta)
    {
        double next = Clamp(_value + delta);
        if (next.Equals(_value))
        {
            return;
        }

        Write(next);
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isWriting)
        {
            return;
        }

        // A half-typed "-" or "" is not an error, it is someone mid-keystroke. Leave the stored
        // value alone until the text means something; LostFocus tidies up whatever is left.
        if (!TryParse(_entry.Text, out double parsed))
        {
            return;
        }

        double clamped = Clamp(parsed);
        if (clamped.Equals(_value))
        {
            return;
        }

        _value = clamped;
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnLostFocus(object sender, RoutedEventArgs e) => Write(_value);

    private double Clamp(double value)
    {
        if (double.IsNaN(value))
        {
            value = _minimum ?? 0d;
        }

        if (_isInteger)
        {
            value = Math.Round(value, MidpointRounding.AwayFromZero);
        }

        if (_minimum.HasValue && value < _minimum.Value)
        {
            value = _minimum.Value;
        }

        if (_maximum.HasValue && value > _maximum.Value)
        {
            value = _maximum.Value;
        }

        return value;
    }

    private string Format(double value)
        => value.ToString("F" + _decimals.ToString(CultureInfo.InvariantCulture), CultureInfo.CurrentCulture);

    private static bool TryParse(string text, out double value)
    {
        if (double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands,
            CultureInfo.CurrentCulture, out value))
        {
            return true;
        }

        // A graph-supplied default written as "1.5" must still load on a German machine.
        return double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands,
            CultureInfo.InvariantCulture, out value);
    }
}
