using System;
using System.Linq;

namespace Interlude.Check;

/// <summary>
/// <c>interlude-check &lt;file-or-folder&gt;…</c>
///
/// Reports what is wrong with a form file, and exits non-zero when anything is. It is meant to be
/// run by whatever wrote the form — a script, CI, or the authoring skill this ships with — so the
/// exit code is the interface and the text is for the person reading afterwards.
/// </summary>
internal static class Program
{
    private const string Usage = """
        interlude-check — checks Interlude form JSON without showing it.

          interlude-check <file-or-folder>...

        Each argument is a form file or a folder of them. Reports duplicate keys, conditions
        naming fields that do not exist, computed values that depend on each other in a loop,
        and anything the reader itself refuses. Exits 0 when every form checked out, 1 otherwise.
        """;

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length == 0 || args.Any(IsHelp))
        {
            Console.WriteLine(Usage);
            return args.Length == 0 ? 1 : 0;
        }

        // Worst result wins: one bad form in a run of ten is a failed run.
        int exit = 0;

        foreach (string path in args)
        {
            exit = Math.Max(exit, FormChecker.Run(path));
        }

        return exit;
    }

    private static bool IsHelp(string argument)
        => argument is "-h" or "--help" or "-?" or "/?";
}
