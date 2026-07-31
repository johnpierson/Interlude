using Autodesk.DesignScript.Runtime;

namespace Interlude.Model;

/// <summary>
/// Per-element presentation overrides. Every member is optional; anything left unset is decided
/// by the active <see cref="Interlude.Theming.ThemeDefinition"/>, which is where consistent
/// forms come from. Reach for this to solve a specific layout problem, not to style a whole form.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record ElementStyle
{
    /// <summary>An all-defaults style, useful as a starting point for <c>with</c> expressions.</summary>
    public static readonly ElementStyle Empty = new();

    public double? Width { get; init; }

    public double? Height { get; init; }

    public double? MinWidth { get; init; }

    public double? MinHeight { get; init; }

    public double? MaxWidth { get; init; }

    public double? MaxHeight { get; init; }

    public Edges? Margin { get; init; }

    public Edges? Padding { get; init; }

    public HorizontalPlacement? HorizontalPlacement { get; init; }

    public VerticalPlacement? VerticalPlacement { get; init; }

    public double? FontSize { get; init; }

    public TextWeight? FontWeight { get; init; }

    public string? FontFamily { get; init; }

    public RgbColor? Foreground { get; init; }

    public RgbColor? Background { get; init; }

    /// <summary>Row index inside a <see cref="GridElement"/>. Ignored elsewhere.</summary>
    public int? GridRow { get; init; }

    /// <summary>Column index inside a <see cref="GridElement"/>. Ignored elsewhere.</summary>
    public int? GridColumn { get; init; }

    public int? GridRowSpan { get; init; }

    public int? GridColumnSpan { get; init; }

    /// <summary>Which edge to attach to inside a <see cref="DockElement"/>. Ignored elsewhere.</summary>
    public DockSide? Dock { get; init; }

    /// <summary>
    /// Width of the label column for this input, overriding the form-wide value.
    /// Set to 0 to place the label above the control instead of beside it.
    /// </summary>
    public double? LabelWidth { get; init; }

    /// <summary>True when nothing on this style would change how the element looks.</summary>
    public bool IsEmpty => Equals(Empty);
}
