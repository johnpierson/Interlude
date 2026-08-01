using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Autodesk.DesignScript.Runtime;
using Interlude.Model;

namespace Interlude.Preview;

/// <summary>
/// Generates one Dynamo node help file per Interlude node.
///
/// Dynamo's documentation browser looks in a package's <c>doc/</c> folder for a Markdown file named
/// after the node — <c>Interlude.Input.TextBox.md</c> — and renders it in the panel beside the
/// graph. The format followed here is Dynamo's own, taken from the fallback docs that ship with
/// Dynamo Core: an <c>## In Depth</c> heading, prose that names the node in backticks, a bullet per
/// input, and an optional <c>## Example File</c> section carrying an image.
///
/// Everything is read from the shipped assembly: the signatures by reflection, the prose from the
/// XML documentation the compiler emits beside it. Nothing is written by hand, so a node that gains
/// a port gains a documented port, and one whose summary is reworded ships the new wording. Help
/// that disagrees with the node it documents is worse than no help at all — the reader believes it.
/// </summary>
internal static class NodeDocs
{
    /// <summary>
    /// Overloads share a method name, so the file name has to carry the parameters as well.
    /// Dynamo's own convention for this is <c>CoordinateSystem.ByOrigin(x, y).md</c> — parameter
    /// names, comma-separated, in brackets — and its browser resolves them the same way.
    /// </summary>
    private const string OverloadFormat = "{0}({1})";

    internal static void Generate(string folder)
    {
        Directory.CreateDirectory(folder);

        Assembly assembly = typeof(FormDefinition).Assembly;
        XmlDocs docs = XmlDocs.LoadBeside(assembly);

        MethodInfo[] nodes = NodeMethods(assembly).ToArray();

        // Which names need the parameter suffix. Worked out per class, because two classes may
        // legitimately both have a Create.
        HashSet<string> overloaded = nodes
            .GroupBy(node => node.DeclaringType!.FullName + "." + node.Name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.Ordinal);

        HashSet<string> current = new(StringComparer.OrdinalIgnoreCase);

        foreach (MethodInfo node in nodes.OrderBy(NodeName, StringComparer.Ordinal))
        {
            string name = FileNameFor(node, overloaded) + ".md";
            File.WriteAllText(Path.Combine(folder, name), Compose(node, docs, folder));
            current.Add(name);
        }

        // A node retired with [IsVisibleInDynamoLibrary(false)] stops being generated but its file
        // would sit here forever, shipping help for something nobody can place. Writing files only
        // ever adds, so the sweep is what makes regenerating the folder mean what it says.
        //
        // Scoped to our own prefix so the folder's README, and any screenshots added by hand,
        // survive it.
        int removed = 0;

        foreach (string stale in Directory.EnumerateFiles(folder, "Interlude.*.md")
            .Where(path => !current.Contains(Path.GetFileName(path))))
        {
            File.Delete(stale);
            removed++;
        }

        Console.WriteLine($"wrote {current.Count} node help files to {folder}" +
            (removed > 0 ? $" ({removed} stale removed)" : string.Empty));
    }

    private static string FileNameFor(MethodInfo node, HashSet<string> overloaded)
    {
        string full = NodeName(node);

        if (!overloaded.Contains(node.DeclaringType!.FullName + "." + node.Name))
        {
            return full;
        }

        string parameters = string.Join(", ", node.GetParameters().Select(p => p.Name));
        return string.Format(CultureInfo.InvariantCulture, OverloadFormat, full, parameters);
    }

    private static string NodeName(MethodInfo node)
        => node.DeclaringType!.FullName + "." + node.Name;

    /// <summary>The name as it reads in a graph: <c>Input.TextBox</c>, without the namespace.</summary>
    private static string ShortName(MethodInfo node)
        => node.DeclaringType!.Name + "." + node.Name;

    private static string Compose(MethodInfo node, XmlDocs docs, string folder)
    {
        MemberDoc doc = docs.For(node);
        ParameterInfo[] parameters = node.GetParameters();

        StringBuilder page = new();
        page.AppendLine("## In Depth");
        page.AppendLine();

        // The signature first, in backticks, the way Dynamo's own pages introduce a node. It is
        // the one thing a reader checks before anything else: what goes in, in what order.
        page.Append('`').Append(Signature(node)).AppendLine("`");
        page.AppendLine();

        if (!string.IsNullOrWhiteSpace(doc.Summary))
        {
            page.AppendLine(doc.Summary);
            page.AppendLine();
        }

        if (parameters.Length > 0)
        {
            page.AppendLine("The inputs are:");
            page.AppendLine();

            foreach (ParameterInfo parameter in parameters)
            {
                page.Append("- `").Append(parameter.Name).Append('`');
                page.Append(" (_").Append(FriendlyName(parameter.ParameterType)).Append('_');

                string? given = DefaultOf(parameter);
                if (given is not null)
                {
                    page.Append(", defaults to `").Append(given).Append('`');
                }

                page.Append(") — ");
                page.AppendLine(doc.Parameter(parameter.Name!) ?? "See the node reference.");
            }

            page.AppendLine();
        }

        page.AppendLine(Outputs(node, doc));

        if (doc.Search.Count > 0)
        {
            page.AppendLine();
            page.Append("Search terms: ")
                .AppendLine(string.Join(", ", doc.Search.Select(term => "`" + term + "`")) + ".");
        }

        // Dynamo's pages end with an example graph and a picture of it. An image reference to a
        // file that is not there renders as a broken image in the browser panel, which looks like
        // a packaging fault — so the section appears only once the picture does.
        string image = NodeName(node) + "_img.png";
        if (File.Exists(Path.Combine(folder, image)))
        {
            page.AppendLine();
            page.AppendLine("___");
            page.AppendLine("## Example File");
            page.AppendLine();
            page.Append("![").Append(ShortName(node)).Append("](./").Append(image).AppendLine(")");
        }

        return page.ToString();
    }

    private static string Outputs(MethodInfo node, MemberDoc doc)
    {
        MultiReturnAttribute? multi = node.GetCustomAttribute<MultiReturnAttribute>();

        if (multi is not null)
        {
            StringBuilder text = new();
            text.AppendLine("The outputs are:");
            text.AppendLine();

            foreach (string key in multi.ReturnKeys)
            {
                text.Append("- `").Append(key).Append("` — ")
                    .AppendLine(doc.Returns(key) ?? "See the node reference.");
            }

            return text.ToString().TrimEnd();
        }

        string name = doc.ReturnName ?? "result";
        string description = doc.Returns(name) ?? FriendlyName(node.ReturnType) + ".";

        return $"Returns `{name}` — {description}";
    }

    private static string Signature(MethodInfo node)
    {
        string parameters = string.Join(", ", node.GetParameters().Select(parameter =>
        {
            string? given = DefaultOf(parameter);
            return given is null ? parameter.Name! : $"{parameter.Name}: {given}";
        }));

        return $"{ShortName(node)}({parameters})";
    }

    private static string? DefaultOf(ParameterInfo parameter)
    {
        if (parameter.GetCustomAttribute<DefaultArgumentAttribute>() is { } dynamoDefault)
        {
            return dynamoDefault.ArgumentExpression;
        }

        if (!parameter.HasDefaultValue)
        {
            return null;
        }

        return parameter.DefaultValue switch
        {
            null => "null",
            string text => "\"" + text + "\"",
            bool flag => flag ? "true" : "false",
            object value => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "?",
        };
    }

    private static string FriendlyName(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            return FriendlyName(underlying);
        }

        if (type.IsArray)
        {
            return "list of " + FriendlyName(type.GetElementType()!);
        }

        if (type.IsGenericType)
        {
            string bare = type.Name[..type.Name.IndexOf('`', StringComparison.Ordinal)];
            Type[] arguments = type.GetGenericArguments();

            // A list of somethings reads better as "list of Element" than as "List<Element>" in a
            // sentence, and Dynamo shows it as a list port either way.
            return bare is "List" or "IEnumerable" or "IReadOnlyList" or "IList"
                ? "list of " + FriendlyName(arguments[0])
                : $"{bare} of {string.Join(" and ", arguments.Select(FriendlyName))}";
        }

        return type switch
        {
            _ when type == typeof(string) => "string",
            _ when type == typeof(bool) => "boolean",
            _ when type == typeof(int) => "integer",
            _ when type == typeof(double) => "number",
            _ when type == typeof(object) => "object",
            _ when type == typeof(void) => "nothing",
            _ => type.Name,
        };
    }

    /// <summary>
    /// The same rule the library itself uses: public static methods on a class in the root
    /// namespace. Anything hidden from the library is hidden from the documentation too, or the
    /// browser would offer help for a node nobody can place.
    /// </summary>
    private static IEnumerable<MethodInfo> NodeMethods(Assembly assembly)
        => assembly.GetExportedTypes()
            .Where(type => type.Namespace == "Interlude" && type.IsClass)
            .Where(IsVisible)
            .SelectMany(type => type.GetMethods(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(IsVisible);

    private static bool IsVisible(MemberInfo member)
        => member.GetCustomAttribute<IsVisibleInDynamoLibraryAttribute>() is not { Visible: false };
}
