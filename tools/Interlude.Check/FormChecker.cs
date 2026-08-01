using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Interlude.Model;
using Interlude.Serialization;
using FormNode = Interlude.Form;

namespace Interlude.Check;

/// <summary>
/// Checks form JSON without showing anything.
///
/// This exists so that a form can be written by something other than a person — a script, or the
/// authoring skill in <c>skills/interlude-form</c> — and be told it is wrong before it reaches
/// Dynamo. Without it the first report of a duplicate key is a graph that behaves oddly in Revit,
/// which is a long way from where the mistake was made.
///
/// Two things can be wrong with a form file and they fail in different places, so both are
/// reported the same way. The reader refuses malformed JSON, an unknown <c>$type</c> or a
/// <c>schemaVersion</c> it does not understand. Beyond that a file can be perfectly well-formed
/// and still not be a working form: two fields sharing a key, a condition naming a field that is
/// not there, computed values that depend on each other in a loop. The second set is exactly what
/// the <c>Form.Check</c> node reports, and this calls that node rather than reimplementing it — a
/// checker that disagrees with the node users run is worse than none.
///
/// No window is created and no form is rendered, so this is safe to run in CI.
/// </summary>
internal static class FormChecker
{
    /// <summary>
    /// Checks one file, or every <c>*.json</c> file in a folder.
    /// </summary>
    /// <returns>A process exit code: 0 when every form checked out, 1 otherwise.</returns>
    internal static int Run(string path)
    {
        string[] files;

        if (Directory.Exists(path))
        {
            // Ordered, because the output is read as a report and a report whose lines move
            // between runs is hard to diff.
            files = Directory.GetFiles(path, "*.json", SearchOption.TopDirectoryOnly)
                .OrderBy(file => file, StringComparer.Ordinal)
                .ToArray();

            if (files.Length == 0)
            {
                Console.Error.WriteLine($"No .json files in {path}.");
                return 1;
            }
        }
        else if (File.Exists(path))
        {
            files = new[] { path };
        }
        else
        {
            Console.Error.WriteLine($"There is no file or folder at {path}.");
            return 1;
        }

        int failed = 0;

        foreach (string file in files)
        {
            if (!CheckOne(file))
            {
                failed++;
            }
        }

        if (files.Length > 1)
        {
            Console.WriteLine();
            Console.WriteLine(failed == 0
                ? $"{files.Length} forms, no problems."
                : $"{files.Length} forms, {failed} with problems.");
        }

        return failed == 0 ? 0 : 1;
    }

    private static bool CheckOne(string file)
    {
        string name = Path.GetFileName(file);
        FormDefinition definition;

        try
        {
            definition = FormJson.Load(file);
        }
        // InvalidOperationException rather than InterludeException: the reader's own exception type
        // is internal to the shipped assembly, which is right — it is not part of the node surface —
        // and it derives from this one.
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or IOException)
        {
            // The reader's own message says what it objected to and, for JSON errors, where. It is
            // better than anything that could be written here, so it is passed through unchanged.
            Report(name, new[] { ex.Message });
            return false;
        }

        Dictionary<string, object> result = FormNode.Check(definition);

        if (result["isValid"] is true)
        {
            Console.WriteLine($"ok    {name}");
            return true;
        }

        Report(name, result["messages"] as IEnumerable<string> ?? Array.Empty<string>());
        return false;
    }

    /// <summary>
    /// Problems go to stderr and the ok lines to stdout, so a caller that only wants to know what
    /// is wrong can discard the rest.
    /// </summary>
    private static void Report(string name, IEnumerable<string> messages)
    {
        Console.Error.WriteLine($"FAIL  {name}");

        foreach (string message in messages)
        {
            Console.Error.WriteLine($"        {message}");
        }
    }
}
