using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Autodesk.DesignScript.Runtime;
using Interlude.Model;

namespace Interlude.Rendering.Wpf.Controls;

/// <summary>
/// A calendar field, with a time box beside it when the element asks for one.
///
/// Dates are shown and typed in the user's own culture — a German user expects 24.12.2026 —
/// while the value handed to the session is a <see cref="DateTime"/>, which carries no format at
/// all. That split is the whole culture story: format at the edges, store neutrally.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class DateTimeField : Grid
{
    private readonly DatePicker _picker;
    private readonly TextBox? _time;

    private bool _isWriting;

    internal DateTimeField(DatePickerElement element)
    {
        ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _picker = new DatePicker
        {
            DisplayDateStart = element.Minimum,
            DisplayDateEnd = element.Maximum,
            SelectedDateFormat = DatePickerFormat.Short,
        };

        _picker.SelectedDateChanged += (_, _) => RaiseIfUserDriven();
        SetColumn(_picker, 0);
        Children.Add(_picker);

        if (element.IncludeTime)
        {
            _time = new TextBox
            {
                Width = 68,
                Margin = new Thickness(6, 0, 0, 0),
                VerticalContentAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
            };
            FieldState.SetPlaceholder(_time, "hh:mm");

            _time.LostFocus += (_, _) => RaiseIfUserDriven();
            _time.KeyDown += (_, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                {
                    RaiseIfUserDriven();
                }
            };

            SetColumn(_time, 1);
            Children.Add(_time);
        }
    }

    /// <summary>Raised when the user changes the date or time.</summary>
    internal event EventHandler? DateChanged;

    /// <summary>The chosen moment, or null when the field is empty.</summary>
    internal DateTime? Read()
    {
        if (_picker.SelectedDate is not DateTime date)
        {
            return null;
        }

        if (_time is null)
        {
            return date.Date;
        }

        return TryParseTime(_time.Text, out TimeSpan time) ? date.Date + time : date.Date;
    }

    /// <summary>Sets the moment without raising <see cref="DateChanged"/>.</summary>
    internal void Write(DateTime? value)
    {
        _isWriting = true;
        try
        {
            _picker.SelectedDate = value?.Date;

            if (_time is not null)
            {
                _time.Text = value.HasValue
                    ? value.Value.ToString("HH:mm", CultureInfo.CurrentCulture)
                    : string.Empty;
            }
        }
        finally
        {
            _isWriting = false;
        }
    }

    private void RaiseIfUserDriven()
    {
        if (!_isWriting)
        {
            DateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Accepts "9:30", "09:30" and "0930", in the user's culture or the invariant one. People
    /// type times loosely, and rejecting a legible one is worse than accepting a sloppy one.
    /// </summary>
    private static bool TryParseTime(string text, out TimeSpan time)
    {
        string trimmed = (text ?? string.Empty).Trim();

        if (TimeSpan.TryParse(trimmed, CultureInfo.CurrentCulture, out time) ||
            TimeSpan.TryParse(trimmed, CultureInfo.InvariantCulture, out time))
        {
            return true;
        }

        if (trimmed.Length == 4 && int.TryParse(trimmed, NumberStyles.None, CultureInfo.InvariantCulture, out int digits))
        {
            int hours = digits / 100;
            int minutes = digits % 100;

            if (hours < 24 && minutes < 60)
            {
                time = new TimeSpan(hours, minutes, 0);
                return true;
            }
        }

        time = default;
        return false;
    }
}
