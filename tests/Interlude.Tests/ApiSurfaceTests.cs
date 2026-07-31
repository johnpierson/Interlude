using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Autodesk.DesignScript.Runtime;
using Xunit;

namespace Interlude.Tests;

/// <summary>
/// Locks the node API down.
///
/// Once a graph is saved, it references nodes by name and binds inputs by position. Renaming a
/// method, reordering a parameter, retyping a port or removing a name from a MultiReturn silently
/// breaks every saved graph that used it — and the breakage surfaces in someone else's project,
/// months later, as a node that will not load.
///
/// So the surface is append-only: new optional parameters go on the end, new MultiReturn names go
/// on the end, and nothing is ever deleted — obsolete nodes are hidden with
/// [IsVisibleInDynamoLibrary(false)] instead. This test is what makes that a rule rather than an
/// intention. When a change here is deliberate, run the tests with INTERLUDE_UPDATE_API=1 to
/// rewrite the snapshot, then read the diff before committing it.
/// </summary>
public class ApiSurfaceTests
{
    private static string SnapshotPath => Path.Combine(RepoPaths.TestRoot, "api-surface.txt");

    [Fact]
    public void The_node_API_matches_the_checked_in_snapshot()
    {
        string current = DescribeSurface();

        if (!File.Exists(SnapshotPath) || IsUpdateRequested())
        {
            File.WriteAllText(SnapshotPath, current);

            Assert.True(
                !IsUpdateRequested(),
                "The API snapshot was rewritten because INTERLUDE_UPDATE_API is set. " +
                "Review the diff, then unset it and re-run.");
            return;
        }

        string expected = File.ReadAllText(SnapshotPath);

        if (string.Equals(Normalize(expected), Normalize(current), StringComparison.Ordinal))
        {
            return;
        }

        // Writing the actual surface beside the snapshot makes the difference reviewable with a
        // normal diff tool, which a multi-thousand-character assertion message would not be.
        string actualPath = Path.Combine(RepoPaths.TestRoot, "api-surface.actual.txt");
        File.WriteAllText(actualPath, current);

        Assert.Fail(
            "The node API has changed.\n" +
            Summarize(Normalize(expected), Normalize(current)) +
            $"\nThe full current surface was written to {actualPath}.\n" +
            "If the change is intentional and append-only, re-run with INTERLUDE_UPDATE_API=1.");
    }

    [Fact]
    public void The_assembly_version_is_frozen_so_upgrades_never_break_saved_graphs()
    {
        Version? version = typeof(Model.FormDefinition).Assembly.GetName().Version;

        Assert.Equal(new Version(1, 0, 0, 0), version);
    }

    /// <summary>Every node needs documentation: Dynamo shows it as the port and node tooltips.</summary>
    [Fact]
    public void Every_node_is_documented_in_the_generated_XML()
    {
        string xmlPath = Path.ChangeExtension(typeof(Model.FormDefinition).Assembly.Location, ".xml");

        Assert.True(File.Exists(xmlPath),
            $"Interlude.xml is missing at {xmlPath}. Dynamo reads it for port names and tooltips.");

        string documentation = File.ReadAllText(xmlPath);
        List<string> undocumented = new();

        foreach (MethodInfo method in NodeMethods())
        {
            // Doc-comment member names use the CLR form: Interlude.Input.TextBox(...)
            string member = $"M:{method.DeclaringType!.FullName}.{method.Name}(";
            string parameterless = $"M:{method.DeclaringType!.FullName}.{method.Name}\"";

            if (!documentation.Contains(member, StringComparison.Ordinal) &&
                !documentation.Contains(parameterless, StringComparison.Ordinal))
            {
                undocumented.Add($"{method.DeclaringType.Name}.{method.Name}");
            }
        }

        Assert.True(
            undocumented.Count == 0,
            "These nodes have no XML documentation: " + string.Join(", ", undocumented.Distinct()));
    }

    /// <summary>
    /// A MultiReturn node must declare its output names, or Dynamo shows a single unnamed port
    /// and every downstream graph has to guess.
    /// </summary>
    [Fact]
    public void Every_dictionary_returning_node_declares_its_output_names()
    {
        List<string> offenders = NodeMethods()
            .Where(method => method.ReturnType == typeof(Dictionary<string, object>))
            .Where(method => method.GetCustomAttribute<MultiReturnAttribute>() is null)
            .Select(method => $"{method.DeclaringType!.Name}.{method.Name}")
            .ToList();

        Assert.True(
            offenders.Count == 0,
            "These nodes return a dictionary but have no [MultiReturn]: " + string.Join(", ", offenders));
    }

    /// <summary>Builds a stable, sorted description of every public node.</summary>
    private static string DescribeSurface()
    {
        StringBuilder surface = new();
        surface.AppendLine("# Interlude node API surface.");
        surface.AppendLine("# Generated by ApiSurfaceTests. Append-only: see the test for why.");
        surface.AppendLine();

        foreach (IGrouping<Type, MethodInfo> group in NodeMethods()
            .GroupBy(method => method.DeclaringType!)
            .OrderBy(group => group.Key.Name, StringComparer.Ordinal))
        {
            surface.Append("## ").AppendLine(group.Key.Name);

            foreach (string line in group
                .Select(Describe)
                .OrderBy(line => line, StringComparer.Ordinal))
            {
                surface.AppendLine(line);
            }

            surface.AppendLine();
        }

        return surface.ToString();
    }

    private static string Describe(MethodInfo method)
    {
        string parameters = string.Join(", ", method.GetParameters().Select(Describe));

        string returns = FriendlyName(method.ReturnType);

        MultiReturnAttribute? multiReturn = method.GetCustomAttribute<MultiReturnAttribute>();
        if (multiReturn is not null)
        {
            returns = "{" + string.Join(", ", multiReturn.ReturnKeys) + "}";
        }

        return $"  {method.Name}({parameters}) -> {returns}";
    }

    private static string Describe(ParameterInfo parameter)
    {
        string text = $"{FriendlyName(parameter.ParameterType)} {parameter.Name}";

        DefaultArgumentAttribute? dynamoDefault =
            parameter.GetCustomAttribute<DefaultArgumentAttribute>();

        if (dynamoDefault is not null)
        {
            return text + " = " + dynamoDefault.ArgumentExpression;
        }

        if (parameter.HasDefaultValue)
        {
            text += " = " + (parameter.DefaultValue switch
            {
                null => "null",
                string s => "\"" + s + "\"",
                bool b => b ? "true" : "false",
                object value => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "?",
            });
        }

        return text;
    }

    private static string FriendlyName(Type type)
    {
        if (type == typeof(void))
        {
            return "void";
        }

        if (Nullable.GetUnderlyingType(type) is Type underlying)
        {
            return FriendlyName(underlying) + "?";
        }

        if (type.IsGenericType)
        {
            string name = type.Name.Substring(0, type.Name.IndexOf('`'));
            string arguments = string.Join(", ", type.GetGenericArguments().Select(FriendlyName));
            return $"{name}<{arguments}>";
        }

        return type switch
        {
            _ when type == typeof(string) => "string",
            _ when type == typeof(bool) => "bool",
            _ when type == typeof(int) => "int",
            _ when type == typeof(double) => "double",
            _ when type == typeof(object) => "object",
            _ => type.Name,
        };
    }

    private static IEnumerable<MethodInfo> NodeMethods()
        => typeof(Model.FormDefinition).Assembly
            .GetExportedTypes()
            .Where(type => type.Namespace == "Interlude" && type.IsClass)
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly));

    private static bool IsUpdateRequested()
        => string.Equals(
            Environment.GetEnvironmentVariable("INTERLUDE_UPDATE_API"),
            "1",
            StringComparison.Ordinal);

    private static string Normalize(string text)
        => text.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd();

    /// <summary>Reports the added and removed lines, which is what a reviewer needs to see.</summary>
    private static string Summarize(string expected, string actual)
    {
        HashSet<string> before = new(expected.Split('\n'), StringComparer.Ordinal);
        HashSet<string> after = new(actual.Split('\n'), StringComparer.Ordinal);

        IEnumerable<string> removed = before.Except(after).Where(line => line.StartsWith("  ", StringComparison.Ordinal));
        IEnumerable<string> added = after.Except(before).Where(line => line.StartsWith("  ", StringComparison.Ordinal));

        StringBuilder summary = new();

        foreach (string line in removed)
        {
            summary.Append("  REMOVED OR CHANGED:").AppendLine(line);
        }

        foreach (string line in added)
        {
            summary.Append("  ADDED:").AppendLine(line);
        }

        return summary.Length == 0 ? "  (only formatting changed)" : summary.ToString();
    }
}
