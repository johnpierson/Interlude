using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Autodesk.DesignScript.Runtime;
using Interlude.Conditions;
using Interlude.Model;
using Interlude.Runtime;

namespace Interlude.Rendering.Wpf.Controls;

/// <summary>
/// A "pick in the Revit model" field: a summary of what is selected, and a button that steps the
/// form aside while the user picks.
///
/// Stepping aside is the delicate part, and both halves of it are load-bearing:
///
/// * The window is <em>minimised</em>, never hidden. Hiding a window shown with
///   <c>ShowDialog</c> ends the modal session — the form would return, not pause.
/// * WPF's <c>ShowDialog</c> disables <em>every</em> top-level window on the thread, not just
///   the owner — Revit's frame and Dynamo's window both, which is why the model could not take a
///   click however enabled the recorded owner was. So the walk here mirrors what WPF did:
///   every disabled top-level window on the thread is re-enabled for exactly the duration of the
///   pick, remembered, and re-disabled in a <c>finally</c> — because a Revit left enabled behind
///   a modal form is a modality hole someone will fall through.
///
/// The pick itself goes through <see cref="RevitSelectionBridge"/>; this class owns only the
/// window choreography and the summary text.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class ModelSelectionBox : Grid
{
    private const int MaxNamesInSummary = 3;

    private readonly ModelSelectionElement _element;
    private readonly TextBlock _summary;
    private readonly Button _pick;
    private readonly string? _unavailable;

    private object? _value;
    private bool _isPicking;

    internal ModelSelectionBox(ModelSelectionElement element)
    {
        _element = element;
        _unavailable = RevitSelectionBridge.UnavailableReason();

        ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Wrapping rather than trimming: in a narrow form "Only available inside Rev…" reads as
        // a bug, while the same words on two lines read as an explanation.
        _summary = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 6, 0),
        };
        SetColumn(_summary, 0);
        Children.Add(_summary);

        _pick = new Button
        {
            Content = string.IsNullOrWhiteSpace(element.ButtonText) ? "Select in model…" : element.ButtonText,
            Padding = new Thickness(10, 0, 10, 0),
            MinWidth = 76,
        };
        _pick.Click += OnPick;
        SetColumn(_pick, 1);
        Children.Add(_pick);

        if (_unavailable is not null)
        {
            _pick.IsEnabled = false;
            _pick.ToolTip = _unavailable;
        }

        UpdateSummary();
    }

    /// <summary>Raised when a pick replaces the value.</summary>
    internal event EventHandler? ValueChanged;

    /// <summary>The picked element or elements, in the shape the element's Coerce produced.</summary>
    internal object? Value
    {
        get => _value;
        set
        {
            _value = value;
            UpdateSummary();
        }
    }

    private void OnPick(object sender, RoutedEventArgs e)
    {
        if (_isPicking || _unavailable is not null)
        {
            return;
        }

        Window? window = Window.GetWindow(this);
        WindowState previousState = window?.WindowState ?? WindowState.Normal;
        List<IntPtr> reEnabled = new();

        _isPicking = true;
        try
        {
            if (window is not null)
            {
                window.WindowState = WindowState.Minimized;
                reEnabled = EnableThreadWindowsForPick(window);
            }

            RevitSelectionOutcome outcome = RevitSelectionBridge.Pick(_element.AllowMultiple, _element.Prompt);

            if (outcome.Failure is not null)
            {
                MessageBox.Show(
                    window,
                    "Selecting in the model did not work: " + outcome.Failure,
                    "Interlude",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (outcome.WasCancelled)
            {
                return;
            }

            Value = _element.Coerce(outcome.Elements);
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            _isPicking = false;

            // Only what this click enabled goes back to disabled: a window that was already
            // enabled — or disabled for someone else's reasons — is not ours to touch.
            foreach (IntPtr handle in reEnabled)
            {
                EnableWindow(handle, false);
            }

            if (window is not null)
            {
                window.WindowState = previousState;
                window.Activate();
            }
        }
    }

    /// <summary>
    /// Re-enables every top-level window on this thread that something — in practice, this
    /// form's own <c>ShowDialog</c> — has disabled, so the Revit view can take the pick clicks.
    /// Returns the handles that were actually flipped, for the <c>finally</c> to flip back.
    /// The dialog itself is skipped: it is minimised, and it is the one window that is meant to
    /// be modal right now.
    /// </summary>
    private static List<IntPtr> EnableThreadWindowsForPick(Window dialog)
    {
        List<IntPtr> flipped = new();
        IntPtr dialogHandle = new WindowInteropHelper(dialog).Handle;

        EnumThreadWindows(
            GetCurrentThreadId(),
            (handle, _) =>
            {
                if (handle != dialogHandle && !IsWindowEnabled(handle))
                {
                    EnableWindow(handle, true);
                    flipped.Add(handle);
                }

                return true;
            },
            IntPtr.Zero);

        return flipped;
    }

    private void UpdateSummary()
    {
        if (_unavailable is not null)
        {
            _summary.Text = _unavailable;
            _summary.Opacity = 0.6;
            return;
        }

        List<object?> items = _value is null
            ? new List<object?>()
            : ValueOps.AsList(_value).Where(item => item is not null).ToList();

        if (items.Count == 0)
        {
            _summary.Text = "Nothing selected yet.";
            _summary.Opacity = 0.6;
            return;
        }

        IEnumerable<string> names = items
            .Take(MaxNamesInSummary)
            .Select(RevitSelectionBridge.Describe)
            .Where(name => name.Length > 0);

        string listed = string.Join(", ", names);
        string suffix = items.Count > MaxNamesInSummary ? ", …" : string.Empty;

        _summary.Text = _element.AllowMultiple
            ? $"{items.Count} selected: {listed}{suffix}"
            : listed;
        _summary.Opacity = 1.0;
    }

    private delegate bool EnumThreadWindowsCallback(IntPtr handle, IntPtr parameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnableWindow(IntPtr handle, [MarshalAs(UnmanagedType.Bool)] bool enable);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowEnabled(IntPtr handle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumThreadWindows(
        uint threadId,
        EnumThreadWindowsCallback callback,
        IntPtr parameter);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
}
