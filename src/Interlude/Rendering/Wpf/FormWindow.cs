using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Autodesk.DesignScript.Runtime;
using Interlude.Model;
using Interlude.Rendering.Wpf.Controls;
using Interlude.Runtime;
using Interlude.Theming;

namespace Interlude.Rendering.Wpf;

/// <summary>
/// The window a form appears in.
///
/// Note how little it does. It builds the tree once, holds the single subscription to
/// <see cref="FormSession.Changed"/>, and applies whatever batches arrive. It contains no rules
/// about when a field is visible, enabled, required or valid — all of that settled in the
/// session before this window existed, and none of it is duplicated here.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public sealed class FormWindow : Window
{
    private readonly FormSession _session;
    private readonly FormDefinition _definition;
    private readonly RenderContext _context;
    private readonly TextBlock _validationSummary;

    private bool _isClosing;

    /// <summary>Builds the window for a form. The session must already be settled.</summary>
    public FormWindow(FormDefinition definition, FormSession session, ControlRendererRegistry registry)
    {
        _definition = definition;
        _session = session;

        ThemePalette palette = WpfThemeApplier.Apply(this, definition.Theme);
        _context = new RenderContext(session, definition.Theme, palette, registry);
        _context.ActionRequested += OnActionRequested;

        Title = string.IsNullOrWhiteSpace(definition.Title) ? "Interlude" : definition.Title;
        WindowStartupLocation = WindowStartupLocation.Manual;
        SizeToContent = definition.Window.Height.HasValue ? SizeToContent.Manual : SizeToContent.Height;
        ShowInTaskbar = definition.Window.ShowInTaskbar;
        Topmost = definition.Window.Topmost;
        ResizeMode = definition.Window.IsResizable ? ResizeMode.CanResize : ResizeMode.NoResize;
        Width = definition.Window.Width;
        MinWidth = definition.Window.MinWidth;
        MinHeight = definition.Window.MinHeight;
        MaxHeight = definition.Window.MaxHeight;
        Background = palette.Background.ToBrush();
        FontFamily = (FontFamily)Resources[ThemeKeys.FontFamily];
        FontSize = definition.Theme.FontSize;
        SnapsToDevicePixels = true;

        if (definition.Window.Height.HasValue)
        {
            Height = definition.Window.Height.Value;
        }

        _validationSummary = BuildValidationSummary();
        Content = BuildLayout();

        ApplyInitialState();

        session.Changed += OnSessionChanged;
        Closed += OnClosed;
        PreviewKeyDown += OnPreviewKeyDown;
        Loaded += OnLoaded;
    }

    /// <summary>The answers, set exactly once before the window closes.</summary>
    public FormResult? Result { get; private set; }

    /// <summary>The built visual tree, keyed by element.</summary>
    public RenderContext Context => _context;

    private FrameworkElement BuildLayout()
    {
        DockPanel root = new() { LastChildFill = true };

        FrameworkElement? header = BuildHeader();
        if (header is not null)
        {
            DockPanel.SetDock(header, Dock.Top);
            root.Children.Add(header);
        }

        FrameworkElement footer = BuildFooter();
        DockPanel.SetDock(footer, Dock.Bottom);
        root.Children.Add(footer);

        StackPanel body = new()
        {
            Margin = new Thickness(_context.Spacing * 2d),
        };

        foreach (FrameworkElement child in _context.BuildChildren(_definition.Elements))
        {
            body.Children.Add(child);
        }

        ScrollViewer scroller = new()
        {
            Content = body,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Focusable = false,
        };

        root.Children.Add(scroller);
        return root;
    }

    private FrameworkElement? BuildHeader()
    {
        bool hasDescription = !string.IsNullOrWhiteSpace(_definition.Description);
        if (!hasDescription)
        {
            // The title bar already shows the title, so a header with nothing but the title
            // repeated is wasted vertical space in a dialog.
            return null;
        }

        StackPanel header = new()
        {
            Margin = new Thickness(_context.Spacing * 2d, _context.Spacing * 2d, _context.Spacing * 2d, 0),
        };

        TextBlock description = new()
        {
            Text = _definition.Description,
            TextWrapping = TextWrapping.Wrap,
        };
        description.SetResourceReference(TextBlock.ForegroundProperty, ThemeKeys.ForegroundMuted);
        header.Children.Add(description);

        return header;
    }

    private FrameworkElement BuildFooter()
    {
        Border footer = new()
        {
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(_context.Spacing * 2d, _context.Spacing, _context.Spacing * 2d, _context.Spacing * 1.5d),
        };
        footer.SetResourceReference(Border.BorderBrushProperty, ThemeKeys.Border);
        footer.SetResourceReference(Border.BackgroundProperty, ThemeKeys.Surface);

        DockPanel row = new() { LastChildFill = false };

        DockPanel.SetDock(_validationSummary, Dock.Left);
        row.Children.Add(_validationSummary);

        StackPanel buttons = new() { Orientation = Orientation.Horizontal };
        DockPanel.SetDock(buttons, Dock.Right);

        if (_definition.Buttons.ShowCancel)
        {
            Button cancel = new()
            {
                Content = _definition.Buttons.CancelText,
                MinWidth = 88,
                IsCancel = false,
                Margin = new Thickness(0, 0, _context.Spacing, 0),
            };
            cancel.Click += (_, _) => CancelForm(FormButtonNames.Cancel);
            buttons.Children.Add(cancel);
        }

        foreach (ButtonElement extra in _definition.Buttons.ExtraButtons)
        {
            Button button = new()
            {
                Content = extra.Text,
                MinWidth = 88,
                Margin = new Thickness(0, 0, _context.Spacing, 0),
            };

            if (extra.IsPrimary)
            {
                button.SetResourceReference(StyleProperty, "Interlude.PrimaryButton");
            }

            ButtonElement captured = extra;
            button.Click += (_, _) => OnActionRequested(this, new FormActionEventArgs(
                captured.Action,
                string.IsNullOrEmpty(captured.Tag) ? captured.Text : captured.Tag,
                captured.Url));

            buttons.Children.Add(button);
        }

        if (_definition.Buttons.ShowSubmit)
        {
            Button submit = new()
            {
                Content = _definition.Buttons.SubmitText,
                MinWidth = 96,
                IsDefault = true,
            };
            submit.SetResourceReference(StyleProperty, "Interlude.PrimaryButton");
            submit.Click += (_, _) => SubmitForm(FormButtonNames.Submit);
            buttons.Children.Add(submit);
        }

        row.Children.Add(buttons);
        footer.Child = row;
        return footer;
    }

    private TextBlock BuildValidationSummary()
    {
        TextBlock summary = new()
        {
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 260,
            Visibility = Visibility.Collapsed,
        };

        summary.SetResourceReference(TextBlock.ForegroundProperty, ThemeKeys.Error);
        summary.SetResourceReference(TextBlock.FontSizeProperty, ThemeKeys.FontSizeSmall);
        return summary;
    }

    private void ApplyInitialState()
    {
        _context.IsApplyingState = true;
        try
        {
            foreach (KeyValuePair<FormElement, ElementView> pair in _context.Views)
            {
                ElementRuntimeState state = _session.GetState(pair.Key);
                pair.Value.Renderer.WriteValue(pair.Value.Control, state.Value);
                pair.Value.ApplyAll(state, revealErrors: false);
            }
        }
        finally
        {
            _context.IsApplyingState = false;
        }
    }

    /// <summary>
    /// The single subscription. Everything the form does in response to an edit arrives here as
    /// one batch, already ordered and already deduplicated by the session.
    /// </summary>
    private void OnSessionChanged(object? sender, FormStateChangedEventArgs e)
    {
        _context.IsApplyingState = true;
        try
        {
            foreach (ElementStateChange change in e.Changes)
            {
                ElementView? view = _context.FindView(change.Element);
                if (view is null)
                {
                    continue;
                }

                if (change.Includes(StateChangeKind.Visibility))
                {
                    view.ApplyVisibility(change.State.IsVisible);
                }

                if (change.Includes(StateChangeKind.Enabled))
                {
                    view.Renderer.ApplyState(view.Control, change.State);
                }

                if (change.Includes(StateChangeKind.Required))
                {
                    view.ApplyRequired(change.State.IsRequired);
                }

                if (change.Includes(StateChangeKind.Value))
                {
                    view.Renderer.WriteValue(view.Control, change.State.Value);
                }

                if (change.Includes(StateChangeKind.Validation) || change.Includes(StateChangeKind.Value))
                {
                    view.ApplyError(change.State.Error, _session.ShowAllErrors || change.State.IsTouched);
                }
            }
        }
        finally
        {
            _context.IsApplyingState = false;
        }

        UpdateValidationSummary();
    }

    private void UpdateValidationSummary()
    {
        if (!_session.ShowAllErrors || _session.IsValid)
        {
            _validationSummary.Visibility = Visibility.Collapsed;
            return;
        }

        int count = _session.Errors.Count;
        _validationSummary.Text = count == 1
            ? "1 field needs attention."
            : $"{count} fields need attention.";
        _validationSummary.Visibility = Visibility.Visible;
    }

    private void OnActionRequested(object? sender, FormActionEventArgs e)
    {
        switch (e.Action)
        {
            case ButtonAction.Submit:
                SubmitForm(FormButtonNames.Submit);
                return;

            case ButtonAction.SubmitWithTag:
                SubmitForm(string.IsNullOrEmpty(e.Tag) ? FormButtonNames.Submit : e.Tag);
                return;

            case ButtonAction.Cancel:
                CancelForm(string.IsNullOrEmpty(e.Tag) ? FormButtonNames.Cancel : e.Tag);
                return;

            case ButtonAction.Reset:
                _session.Reset();
                RefreshEverything();
                return;

            case ButtonAction.OpenUrl:
                MarkdownView.OpenExternal(e.Url);
                return;
        }
    }

    /// <summary>Pushes every current state into every control, after a wholesale change.</summary>
    private void RefreshEverything()
    {
        _context.IsApplyingState = true;
        try
        {
            foreach (KeyValuePair<FormElement, ElementView> pair in _context.Views)
            {
                ElementRuntimeState state = _session.GetState(pair.Key);
                pair.Value.Renderer.WriteValue(pair.Value.Control, state.Value);
                pair.Value.ApplyAll(state, _session.ShowAllErrors);
            }
        }
        finally
        {
            _context.IsApplyingState = false;
        }

        UpdateValidationSummary();
    }

    private void SubmitForm(string buttonClicked)
    {
        if (_session.TrySubmit(buttonClicked, out FormResult? result))
        {
            Result = result;
            _isClosing = true;
            Close();
            return;
        }

        // The session has already revealed every failing field by raising a batch when
        // ShowAllErrors flipped, so all that is left here is to take the user to the first one.
        UpdateValidationSummary();
        FocusFirstInvalid();
    }

    private void CancelForm(string buttonClicked)
    {
        Result = _session.BuildCancelledResult(buttonClicked);
        _isClosing = true;
        Close();
    }

    private void FocusFirstInvalid()
    {
        ElementRuntimeState? invalid = _session.FirstInvalid();
        if (invalid is null)
        {
            return;
        }

        ElementView? view = _context.FindView(invalid.Element);
        view?.Control.BringIntoView();
        view?.Control.Focus();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Focus the first thing the user can actually answer, so the form is usable from the
        // keyboard the moment it appears.
        ElementView? first = _definition.AllElements()
            .Where(element => element is InputElement)
            .Select(_context.FindView)
            .FirstOrDefault(view => view is not null && view.Root.IsVisible && view.Control.Focusable);

        first?.Control.Focus();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _definition.Buttons.CloseOnEscape)
        {
            e.Handled = true;
            CancelForm(FormButtonNames.Cancel);
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        // Unsubscribing matters: the session outlives the window by however long the graph takes
        // to finish, and a live handler would keep the whole visual tree alive with it.
        _session.Changed -= OnSessionChanged;
        _context.ActionRequested -= OnActionRequested;
        Closed -= OnClosed;
        PreviewKeyDown -= OnPreviewKeyDown;
        Loaded -= OnLoaded;

        // Closed with the title-bar X rather than a button: still an answer, still not nulls.
        if (!_isClosing || Result is null)
        {
            Result ??= _session.BuildCancelledResult(FormButtonNames.Closed);
        }
    }
}
