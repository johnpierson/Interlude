using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Interlude.Model;
using Interlude.Rendering.Wpf;
using Interlude.Runtime;
using Interlude.Theming;

namespace Interlude.Preview;

/// <summary>
/// Renders forms to PNG without showing a window.
///
/// Useful twice over: it produces the images for the documentation, and it makes a rendering
/// change reviewable — a pull request that alters spacing or contrast can show what it did rather
/// than describe it. Nothing here is part of the test suite; pixel comparison across machines,
/// display scales and font versions is a source of false failures, not confidence.
/// </summary>
internal static class Screenshots
{
    /// <summary>Renders every gallery sample in both themes into <paramref name="folder"/>.</summary>
    internal static void CaptureGallery(string folder, double scale = 1.5d)
    {
        Directory.CreateDirectory(folder);

        foreach (Sample sample in Gallery.Samples)
        {
            string slug = sample.Name.Replace(' ', '-').ToLowerInvariant();

            foreach ((string suffix, AppearanceMode mode) in new[]
            {
                ("light", AppearanceMode.Light),
                ("dark", AppearanceMode.Dark),
            })
            {
                FormDefinition definition = sample.Build();
                definition = definition with { Theme = definition.Theme with { Mode = mode } };

                string path = Path.Combine(folder, $"{slug}-{suffix}.png");
                Capture(definition, path, scale);
                Console.WriteLine("wrote " + Path.GetFileName(path));
            }
        }
    }

    /// <summary>Renders one form to a PNG.</summary>
    internal static void Capture(FormDefinition definition, string path, double scale = 1.5d)
    {
        FormSession session = new(definition);
        FormWindow window = new(definition, session, ControlRendererRegistry.CreateDefault());

        try
        {
            // Rendered in place, still attached to the window.
            //
            // Detaching the content into a standalone host is the obvious way to give the image a
            // background, and it is wrong: implicit styles and DynamicResource lookups resolve by
            // walking up the logical tree to the window's own Resources, which is exactly where
            // Interlude puts its theme. A detached tree finds nothing and renders as unstyled WPF.
            FrameworkElement root = (FrameworkElement)window.Content;

            if (root is Panel panel)
            {
                panel.Background = window.Background ?? Brushes.White;
            }

            root.Width = definition.Window.Width;
            root.Measure(new Size(definition.Window.Width, double.PositiveInfinity));

            double height = Math.Min(definition.Window.MaxHeight, Math.Max(80d, root.DesiredSize.Height));
            root.Arrange(new Rect(0, 0, definition.Window.Width, height));
            root.UpdateLayout();

            FrameworkElement host = root;

            RenderTargetBitmap bitmap = new(
                (int)Math.Ceiling(definition.Window.Width * scale),
                (int)Math.Ceiling(height * scale),
                96d * scale,
                96d * scale,
                PixelFormats.Pbgra32);

            bitmap.Render(host);

            PngBitmapEncoder encoder = new();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            using FileStream file = File.Create(path);
            encoder.Save(file);
        }
        finally
        {
            window.Close();
        }
    }
}
