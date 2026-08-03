using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Interlude.Preview;

/// <summary>
/// Draws a library icon for every node and writes the resource manifest Dynamo reads them from.
///
/// THE FAMILY SYSTEM. Each icon is a plate and a glyph. The plate's colour says which of the nine
/// categories the node belongs to; the glyph says what the node does, and glyphs are shared freely
/// across categories — a calendar is a calendar whether it is the field that asks for a date, the
/// rule that bounds one, or the node that reads one back out. Colour answers "where am I", shape
/// answers "what is this", and neither has to carry both jobs.
///
/// The alternative was a hundred and twelve unique drawings. In Dynamo's library the icon is drawn
/// at about sixteen pixels: roughly a twelve-pixel interior, which is not enough to separate
/// <c>Condition.GreaterThan</c> from <c>Condition.AtLeast</c> however carefully they are drawn. A
/// hundred and twelve marks at that size are a hundred and twelve smudges. Nine colours and a
/// shared shape vocabulary are legible, and the label beside the icon does the fine distinguishing
/// it is already there to do.
///
/// Dev only. The PNGs and the resource container are generated here and checked in; the shipped
/// icon assembly is a thin wrapper round the container. See docs/architecture.md.
/// </summary>
internal static class Icons
{
    /// <summary>Rendered at the two sizes Dynamo asks for. Small is the library tree; large is the node.</summary>
    private static readonly (string Suffix, int Pixels)[] Sizes =
    {
        ("Small", 32),
        ("Large", 128),
    };

    /// <summary>
    /// One category's colours.
    ///
    /// <c>Paper</c> is what a glyph's interior is filled with so that a white-on-yellow shape stays
    /// readable.
    ///
    /// Eight of the nine plates are loud and one is not. Theme takes the near-black plate with
    /// white line work, which suits a family whose nodes are all about light and dark, and it is
    /// the family that can afford it: seven nodes reached for once per graph. Its shadow is the
    /// accent pink rather than black, because a black shadow on a near-black plate against Dynamo's
    /// dark library background is three shades of nothing.
    ///
    /// Form gets white for the opposite reason. It is the family every graph uses and the one
    /// people hunt for, so it takes the brightest, highest-contrast chip of the set.
    /// </summary>
    private sealed record Family(string Plate, string Ink, string Paper, string Shadow);

    private static readonly IReadOnlyDictionary<string, Family> Families =
        new Dictionary<string, Family>(StringComparer.Ordinal)
        {
            ["Input"] = new("#FF5C8A", "#000000", "#FFFFFF", "#000000"),
            ["Layout"] = new("#FFE55C", "#000000", "#FFFFFF", "#000000"),
            ["Behavior"] = new("#7DD3FC", "#000000", "#FFFFFF", "#000000"),
            ["Condition"] = new("#C7F464", "#000000", "#FFFFFF", "#000000"),
            ["Compute"] = new("#FF9A3C", "#000000", "#FFFFFF", "#000000"),
            ["Rule"] = new("#B98CFF", "#000000", "#FFFFFF", "#000000"),
            ["Theme"] = new("#17171B", "#FFFFFF", "#17171B", "#FF5C8A"),
            ["Form"] = new("#FFFFFF", "#000000", "#FFFFFF", "#000000"),
            ["Result"] = new("#5EEAD4", "#000000", "#FFFFFF", "#000000"),
        };

    private sealed record IconSpec(string Node, Glyph Glyph, Badge Badge = Badge.None)
    {
        internal string Category => Node[..Node.IndexOf('.', StringComparison.Ordinal)];
    }

    /// <summary>
    /// Every node, and what it is drawn as.
    ///
    /// Hand-written rather than derived, because no rule about a node's name produces the right
    /// picture. <c>NodeIconTests</c> checks this against the assembly in both directions, so a node
    /// added without an entry here fails the build rather than shipping without an icon.
    /// </summary>
    private static readonly IconSpec[] Catalogue =
    {
        // ---- Input: the things a person fills in --------------------------------------------
        new("Input.CheckBox", Glyph.CheckBox),
        new("Input.ColorPicker", Glyph.Droplet),
        new("Input.DatePicker", Glyph.Calendar),
        new("Input.DirectoryPath", Glyph.Folder),
        new("Input.DropDown", Glyph.DropChevron),
        new("Input.FilePath", Glyph.Document),
        new("Input.Integer", Glyph.Stepper),
        new("Input.ListBox", Glyph.Stack),
        new("Input.Number", Glyph.StepperDecimal),
        new("Input.Password", Glyph.Dots),
        new("Input.RadioButtons", Glyph.Radio),
        new("Input.Slider", Glyph.Slider),
        new("Input.TextArea", Glyph.Lines),
        new("Input.TextBox", Glyph.Field),
        new("Input.Toggle", Glyph.Toggle),
        new("Input.TreeItem", Glyph.TreeLeaf),
        new("Input.TreeSelect", Glyph.Tree),

        // ---- Layout: structure and decoration -------------------------------------------------
        new("Layout.Button", Glyph.Button),
        new("Layout.Card", Glyph.Card),
        new("Layout.Cell", Glyph.GridCell),
        new("Layout.Column", Glyph.Rows),
        new("Layout.Dock", Glyph.Frame),
        new("Layout.Docked", Glyph.DockedEdge),
        new("Layout.Expander", Glyph.Expander),
        new("Layout.Grid", Glyph.Grid),
        new("Layout.Image", Glyph.Picture),
        new("Layout.Label", Glyph.Lines),
        new("Layout.LinkButton", Glyph.Button, Badge.ArrowOut),
        new("Layout.Markdown", Glyph.Markdown),
        new("Layout.Progress", Glyph.ProgressBar),
        new("Layout.ResetButton", Glyph.Reset),
        new("Layout.Row", Glyph.Columns),
        new("Layout.Scroll", Glyph.Scroll),
        new("Layout.Section", Glyph.HeaderPanel),
        new("Layout.Separator", Glyph.Separator),
        new("Layout.Spacer", Glyph.Spacer),
        new("Layout.Split", Glyph.Split),
        new("Layout.TabPage", Glyph.TabPage),
        new("Layout.Tabs", Glyph.Tabs),

        // ---- Behavior: modifiers wrapped round an element -------------------------------------
        new("Behavior.EnabledIf", Glyph.Power),
        new("Behavior.ReadOnly", Glyph.Lock),
        new("Behavior.Required", Glyph.Asterisk),
        new("Behavior.RequiredIf", Glyph.Asterisk, Badge.Fork),
        new("Behavior.VisibleIf", Glyph.Eye),
        new("Behavior.WithComputed", Glyph.EqualsSign),
        new("Behavior.WithHelp", Glyph.Bubble),
        new("Behavior.WithKey", Glyph.Key),
        new("Behavior.WithSize", Glyph.Resize),
        new("Behavior.WithValidation", Glyph.Shield),

        // ---- Condition: predicates over the answers -------------------------------------------
        new("Condition.Always", Glyph.FilledDisc),
        new("Condition.And", Glyph.VennAnd),
        new("Condition.AtLeast", Glyph.GreaterOrEqual),
        new("Condition.AtMost", Glyph.LessOrEqual),
        new("Condition.Contains", Glyph.Inside),
        new("Condition.EndsWith", Glyph.AnchorEnd),
        new("Condition.Equals", Glyph.EqualsSign),
        new("Condition.GreaterThan", Glyph.Greater),
        new("Condition.In", Glyph.Member),
        new("Condition.IsChecked", Glyph.CheckBox),
        new("Condition.IsEmpty", Glyph.EmptyBox),
        new("Condition.IsNotChecked", Glyph.UncheckedBox, Badge.Slash),
        new("Condition.IsNotEmpty", Glyph.FullBox),
        new("Condition.LessThan", Glyph.Less),
        new("Condition.Matches", Glyph.Wildcard),
        new("Condition.Not", Glyph.FilledDisc, Badge.Slash),
        new("Condition.NotEquals", Glyph.EqualsSign, Badge.Slash),
        new("Condition.Or", Glyph.VennOr),
        new("Condition.StartsWith", Glyph.AnchorStart),

        // ---- Compute: values worked out from other answers ------------------------------------
        new("Compute.Arithmetic", Glyph.Operators),
        new("Compute.Constant", Glyph.Pin),
        new("Compute.Field", Glyph.FieldRef),
        new("Compute.Format", Glyph.Braces),
        new("Compute.If", Glyph.Fork),
        new("Compute.Lookup", Glyph.Table),
        new("Compute.Sum", Glyph.Sigma),

        // ---- Rule: validation ------------------------------------------------------------------
        new("Rule.CompareTo", Glyph.Compare),
        new("Rule.FileExists", Glyph.Document, Badge.Tick),
        new("Rule.FolderExists", Glyph.Folder, Badge.Tick),
        new("Rule.Length", Glyph.Ruler),
        new("Rule.Range", Glyph.Range),
        new("Rule.Regex", Glyph.Wildcard),
        new("Rule.Required", Glyph.Asterisk),

        // ---- Theme: appearance -----------------------------------------------------------------
        new("Theme.Create", Glyph.Palette),
        new("Theme.Dark", Glyph.Moon),
        new("Theme.Light", Glyph.Sun),
        new("Theme.Mono", Glyph.Contrast),
        new("Theme.Neubrutalism", Glyph.HardShadow),
        new("Theme.System", Glyph.Monitor),
        new("Theme.WithColors", Glyph.Swatches),

        // ---- Form: building and showing --------------------------------------------------------
        new("Form.Check", Glyph.Window, Badge.Tick),
        new("Form.Create", Glyph.Window, Badge.Plus),
        new("Form.Forget", Glyph.Window, Badge.Cross),
        new("Form.FromJson", Glyph.Braces, Badge.ArrowIn),
        new("Form.Options", Glyph.Sliders),
        new("Form.Show", Glyph.Window, Badge.Play),
        new("Form.ShowDefinition", Glyph.Document, Badge.Play),
        new("Form.ToJson", Glyph.Braces, Badge.ArrowOut),
        new("Form.WithOptions", Glyph.Stack, Badge.ArrowIn),

        // ---- Result: reading the answers back --------------------------------------------------
        new("Result.ButtonClicked", Glyph.Cursor),
        new("Result.GetBool", Glyph.CheckBox, Badge.ArrowOut),
        new("Result.GetColor", Glyph.Droplet, Badge.ArrowOut),
        new("Result.GetDate", Glyph.Calendar, Badge.ArrowOut),
        new("Result.GetFilePaths", Glyph.Document, Badge.ArrowOut),
        new("Result.GetInteger", Glyph.Stepper, Badge.ArrowOut),
        new("Result.GetList", Glyph.Stack, Badge.ArrowOut),
        new("Result.GetNumber", Glyph.StepperDecimal, Badge.ArrowOut),
        new("Result.GetString", Glyph.Field, Badge.ArrowOut),
        new("Result.HasKey", Glyph.Key, Badge.Tick),
        new("Result.Keys", Glyph.Key),
        new("Result.ValueByKey", Glyph.Key, Badge.ArrowOut),
        new("Result.Values", Glyph.Stack),
        new("Result.WasCancelled", Glyph.CircleCross),
        new("Result.WasSubmitted", Glyph.CircleTick),
    };

    /// <summary>
    /// Writes every icon into <c>Images/</c> under <paramref name="projectFolder"/> and rewrites
    /// the resource manifest beside them.
    /// </summary>
    internal static void Generate(string projectFolder)
    {
        VerifyCatalogueMatchesTheAssembly();

        string images = Path.Combine(projectFolder, "Images");
        Directory.CreateDirectory(images);

        HashSet<string> written = new(StringComparer.OrdinalIgnoreCase);

        foreach (IconSpec spec in Catalogue.OrderBy(s => s.Node, StringComparer.Ordinal))
        {
            Family family = Families[spec.Category];

            foreach ((string suffix, int pixels) in Sizes)
            {
                string name = $"Interlude.{spec.Node}.{suffix}.png";
                Render(spec, family, pixels, Path.Combine(images, name));
                written.Add(name);
            }
        }

        // A renamed node would otherwise leave its old icon behind, and the resource manifest is
        // generated from the catalogue rather than from the folder, so it would sit there unnoticed
        // and get committed.
        foreach (string stale in Directory.GetFiles(images, "Interlude.*.png")
                     .Where(path => !written.Contains(Path.GetFileName(path))))
        {
            File.Delete(stale);
            Console.WriteLine("removed " + Path.GetFileName(stale));
        }

        WriteResourceContainer(
            Path.Combine(projectFolder, "InterludeImages.resources"),
            images,
            written.OrderBy(name => name, StringComparer.Ordinal));

        Console.WriteLine($"wrote {written.Count} icons for {Catalogue.Length} nodes");
    }

    /// <summary>
    /// The catalogue must name every node in the assembly and no others.
    ///
    /// Checked here as well as in the test suite, because the generator is what CI runs to detect
    /// drift: without this a new node would quietly produce a manifest missing an entry, and the
    /// only symptom would be one node in the library wearing Dynamo's default cube.
    /// </summary>
    private static void VerifyCatalogueMatchesTheAssembly()
    {
        HashSet<string> declared = Catalogue.Select(spec => spec.Node).ToHashSet(StringComparer.Ordinal);
        HashSet<string> actual = NodeNames().ToHashSet(StringComparer.Ordinal);

        string[] missing = actual.Except(declared, StringComparer.Ordinal).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        string[] extra = declared.Except(actual, StringComparer.Ordinal).OrderBy(n => n, StringComparer.Ordinal).ToArray();

        if (missing.Length > 0 || extra.Length > 0)
        {
            throw new InvalidOperationException(
                "The icon catalogue disagrees with the assembly.\n" +
                (missing.Length > 0 ? "  nodes with no icon: " + string.Join(", ", missing) + "\n" : string.Empty) +
                (extra.Length > 0 ? "  icons for nodes that do not exist: " + string.Join(", ", extra) : string.Empty));
        }
    }

    /// <summary>Every zero-touch node, as Dynamo names it: the facade class and the method.</summary>
    internal static IEnumerable<string> NodeNames()
        => typeof(Form).Assembly
            .GetExportedTypes()
            .Where(type => type.Namespace == "Interlude" && type.IsClass)
            .SelectMany(type => type
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Select(method => type.Name + "." + method.Name))
            .Distinct(StringComparer.Ordinal);

    private static void Render(IconSpec spec, Family family, int pixels, string path)
    {
        Brush plate = Fill(family.Plate);
        Brush ink = Fill(family.Ink);
        Brush paper = Fill(family.Paper);
        Brush shadow = Fill(family.Shadow);

        DrawingVisual visual = new();

        using (DrawingContext dc = visual.RenderOpen())
        {
            double scale = pixels / IconGeometry.Grid;
            dc.PushTransform(new ScaleTransform(scale, scale));

            IconGeometry g = new(dc, ink, paper);

            // The hard offset shadow, the plate, then the drawing on top. No blur anywhere: the
            // shadow is a second solid rectangle, which is the whole idea.
            g.Block(4, 4, 27, 27, shadow);
            g.Box(1.5, 1.5, 27, 27, plate, new Pen(ink, 2.4d));

            IconGlyphs.Draw(g, spec.Glyph);
            IconGlyphs.DrawBadge(g, spec.Badge);

            dc.Pop();
        }

        RenderTargetBitmap bitmap = new(pixels, pixels, 96d, 96d, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using FileStream file = File.Create(path);
        encoder.Save(file);
    }

    private static Brush Fill(string hex)
    {
        SolidColorBrush brush = new((Color)ColorConverter.ConvertFromString(hex)!);
        brush.Freeze();
        return brush;
    }

    /// <summary>
    /// Writes the compiled resource container the icon assembly embeds.
    ///
    /// WRITTEN HERE RATHER THAN COMPILED FROM A .resx, and the reason is the whole format.
    ///
    /// Every entry is raw PNG bytes, which <see cref="ResourceWriter"/> stores as
    /// <c>ResourceTypeCode.ByteArray</c> under the classic <c>System.Resources.ResourceReader</c>
    /// header. That is byte-for-byte the shape of Dynamo's own customization assemblies —
    /// DSCoreNodes, ProtoGeometry and the rest all read back with a plain ResourceReader.
    ///
    /// Going through a .resx does not produce that. MSBuild refuses to put non-string resources in
    /// a .resources file unless <c>GenerateResourceUsePreserializedResources</c> is set, and that
    /// switch writes a header naming <c>System.Resources.Extensions.DeserializingResourceReader</c>
    /// instead. A plain ResourceReader does not merely ignore that header, it throws on it, so the
    /// icons would have been unreadable by the host they exist for. Tried, measured, discarded.
    ///
    /// The other route — entries typed <c>System.Drawing.Bitmap</c>, which is what the older Dynamo
    /// packages on this machine still use — is a worse dead end: those go through BinaryFormatter,
    /// which .NET 9 removed outright.
    /// </summary>
    private static void WriteResourceContainer(string path, string imageFolder, IEnumerable<string> imageNames)
    {
        using FileStream file = File.Create(path);
        using ResourceWriter writer = new(file);

        foreach (string image in imageNames)
        {
            // The key is the name Dynamo looks up: the node's fully qualified name and the size,
            // which is exactly the file name without its extension.
            writer.AddResource(
                Path.GetFileNameWithoutExtension(image),
                File.ReadAllBytes(Path.Combine(imageFolder, image)));
        }

        writer.Generate();
    }
}
