using Autodesk.DesignScript.Runtime;

namespace Interlude.Model;

/// <summary>A run of static text.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record LabelElement : DisplayElement
{
    public string Text { get; init; } = string.Empty;

    /// <summary>1 through 4 render as headings; 0 is body text.</summary>
    public int HeadingLevel { get; init; }

    public bool WrapText { get; init; } = true;

    /// <summary>Renders in the muted colour, for captions and asides.</summary>
    public bool IsMuted { get; init; }
}

/// <summary>
/// A block of lightweight Markdown: headings, bold, italic, inline code, links, bullet and
/// numbered lists, and rules. This is a small, predictable subset rendered natively rather
/// than a full CommonMark implementation, which would mean either a dependency or a parser
/// far larger than the rest of the package.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record MarkdownElement : DisplayElement
{
    public string Text { get; init; } = string.Empty;
}

/// <summary>
/// A picture, given either as a path or as raw bytes. Never a <c>BitmapSource</c>: the model
/// stays free of WPF so it can be serialized and tested without a UI thread.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record ImageElement : DisplayElement
{
    /// <summary>Absolute or relative path to an image file.</summary>
    public string? Path { get; init; }

    /// <summary>Image bytes, used when <see cref="Path"/> is not set.</summary>
    public byte[]? Bytes { get; init; }

    public ImageFit Fit { get; init; } = ImageFit.Contain;

    /// <summary>Text for assistive technology.</summary>
    public string? AlternateText { get; init; }
}

/// <summary>A dividing line.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record SeparatorElement : DisplayElement
{
    public LayoutOrientation Orientation { get; init; } = LayoutOrientation.Horizontal;

    /// <summary>Optional caption drawn on the line.</summary>
    public string? Caption { get; init; }
}

/// <summary>Blank space.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record SpacerElement : DisplayElement
{
    /// <summary>Height for a vertical layout, width for a horizontal one.</summary>
    public double Size { get; init; } = 8d;
}

/// <summary>A progress bar. Static unless something in the graph updates the form.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record ProgressElement : DisplayElement
{
    public double Value { get; init; }

    public double Minimum { get; init; }

    public double Maximum { get; init; } = 100d;

    /// <summary>Shows a looping animation instead of a filled portion.</summary>
    public bool IsIndeterminate { get; init; }

    public bool ShowPercentage { get; init; } = true;

    /// <summary>
    /// Draws the bar as this many discrete cells instead of one continuous fill. Zero, the
    /// default, is continuous.
    ///
    /// Segments are for counting rather than measuring: "five of seven days" reads off a
    /// segmented bar at a glance, where a continuous bar at 71% does not.
    /// </summary>
    public int Segments { get; init; }
}

/// <summary>
/// A button inside the form body, separate from the submit and cancel buttons in the footer.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record ButtonElement : DisplayElement
{
    public string Text { get; init; } = "Button";

    public ButtonAction Action { get; init; } = ButtonAction.SubmitWithTag;

    /// <summary>
    /// Reported as <c>buttonClicked</c> when this button closes the form, which is how one form
    /// offers several outcomes ("Place", "Place and Continue", "Preview").
    /// </summary>
    public string Tag { get; init; } = string.Empty;

    /// <summary>Opened for <see cref="ButtonAction.OpenUrl"/>.</summary>
    public string? Url { get; init; }

    /// <summary>Draws the button in the accent colour, marking it as the primary action.</summary>
    public bool IsPrimary { get; init; }

    public string? IconPath { get; init; }
}
