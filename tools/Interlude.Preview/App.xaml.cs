using System;
using System.Globalization;
using System.IO;
using System.Windows;
using Interlude.Model;
using Interlude.Serialization;

namespace Interlude.Preview;

/// <summary>The harness application.</summary>
public partial class App : Application
{
    /// <summary>
    /// Runs the harness, unless asked to export the gallery instead.
    ///
    /// <c>Interlude.Preview.exe --export &lt;folder&gt;</c> writes every gallery sample as JSON.
    /// Those files are checked in and validated by the test suite, which is what stops the
    /// documented example forms from drifting away from the schema that reads them.
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // The main window is created here rather than through StartupUri, because StartupUri is
        // honoured whatever OnStartup does — the export run would put a window on screen and then
        // wait for someone to close it.
        if (e.Args.Length >= 2 &&
            string.Equals(e.Args[0], "--export", StringComparison.OrdinalIgnoreCase))
        {
            Export(e.Args[1]);
            Shutdown(0);
            return;
        }

        MainWindow window = new();
        MainWindow = window;
        window.Show();
    }

    private static void Export(string folder)
    {
        Directory.CreateDirectory(folder);

        foreach (Sample sample in Gallery.Samples)
        {
            string name = sample.Name.Replace(' ', '-').ToLowerInvariant() + ".json";
            FormDefinition definition = sample.Build();

            FormJson.Save(definition, Path.Combine(folder, name));
            Console.WriteLine("wrote " + name);
        }
    }
}
