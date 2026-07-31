using System;
using System.IO;

namespace Interlude.Tests;

/// <summary>Locates the repository from the test output folder.</summary>
internal static class RepoPaths
{
    /// <summary>The repository root, found by walking up to the file that marks it.</summary>
    internal static string Root { get; } = FindRoot();

    /// <summary>The shipped project's source folder.</summary>
    internal static string SourceRoot => Path.Combine(Root, "src", "Interlude");

    /// <summary>The test project's folder, where checked-in fixtures live.</summary>
    internal static string TestRoot => Path.Combine(Root, "tests", "Interlude.Tests");

    private static string FindRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "versions.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "Could not find the repository root: no versions.json above " + AppContext.BaseDirectory);
    }
}
