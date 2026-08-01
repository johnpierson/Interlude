using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Interlude.Preview;

/// <summary>The documentation the compiler emitted for one node.</summary>
internal sealed class MemberDoc
{
    internal static readonly MemberDoc Empty = new();

    internal string Summary { get; init; } = string.Empty;

    /// <summary>The name on the single <c>returns</c> tag, which Dynamo shows as the port label.</summary>
    internal string? ReturnName { get; init; }

    internal IReadOnlyList<string> Search { get; init; } = Array.Empty<string>();

    internal IReadOnlyDictionary<string, string> Parameters { get; init; }
        = new Dictionary<string, string>(StringComparer.Ordinal);

    internal IReadOnlyDictionary<string, string> ReturnDescriptions { get; init; }
        = new Dictionary<string, string>(StringComparer.Ordinal);

    internal string? Parameter(string name)
        => Parameters.TryGetValue(name, out string? text) ? text : null;

    internal string? Returns(string name)
        => ReturnDescriptions.TryGetValue(name, out string? text) ? text : null;
}

/// <summary>
/// Reads the XML documentation file the compiler writes beside the assembly.
///
/// This is the same file Dynamo itself reads for port names and tooltips, which is the point: the
/// generated help and the tooltip a user sees when hovering a port come from one source, so they
/// cannot say different things.
/// </summary>
internal sealed class XmlDocs
{
    private readonly Dictionary<string, MemberDoc> _members;

    private XmlDocs(Dictionary<string, MemberDoc> members) => _members = members;

    internal static XmlDocs LoadBeside(Assembly assembly)
    {
        string path = Path.ChangeExtension(assembly.Location, ".xml");

        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Interlude.xml is not beside the assembly at {path}. The documentation is " +
                "generated from it, so build the project with GenerateDocumentationFile before " +
                "generating node help.",
                path);
        }

        Dictionary<string, MemberDoc> members = new(StringComparer.Ordinal);

        foreach (XElement member in XDocument.Load(path).Descendants("member"))
        {
            string? name = member.Attribute("name")?.Value;

            // Methods for the nodes themselves, types for the family summary each one carries.
            if (name is null ||
                !(name.StartsWith("M:", StringComparison.Ordinal) ||
                  name.StartsWith("T:", StringComparison.Ordinal)))
            {
                continue;
            }

            members[name] = Read(member);
        }

        return new XmlDocs(members);
    }

    internal MemberDoc For(MethodInfo method)
        => _members.TryGetValue(KeyOf(method), out MemberDoc? doc) ? doc : MemberDoc.Empty;

    /// <summary>
    /// The summary on the class a node belongs to.
    ///
    /// Worth having on every page, because that is where the rules shared by a whole family live —
    /// that choice inputs return the object rather than its name, that a rule on a hidden field is
    /// never applied, that every Behavior node returns a new element. A reader who arrived at one
    /// node from the library has not read the class summary anywhere else, and repeating it beats
    /// them not knowing it.
    /// </summary>
    internal string ForType(Type type)
        => _members.TryGetValue("T:" + type.FullName, out MemberDoc? doc) ? doc.Summary : string.Empty;

    /// <summary>
    /// Builds the documentation ID the compiler used as the <c>member name</c> attribute:
    /// <c>M:Interlude.Input.TextBox(System.String,System.String)</c>.
    ///
    /// The rules are the ones in the C# specification's annex on documentation comments — fully
    /// qualified parameter types, no spaces, generic arguments in braces rather than angle
    /// brackets, and no bracket at all when a method takes nothing.
    /// </summary>
    private static string KeyOf(MethodInfo method)
    {
        ParameterInfo[] parameters = method.GetParameters();
        string name = $"M:{method.DeclaringType!.FullName}.{method.Name}";

        return parameters.Length == 0
            ? name
            : name + "(" + string.Join(",", parameters.Select(p => DocIdOf(p.ParameterType))) + ")";
    }

    private static string DocIdOf(Type type)
    {
        if (type.IsArray)
        {
            return DocIdOf(type.GetElementType()!) + "[]";
        }

        if (type.IsByRef)
        {
            return DocIdOf(type.GetElementType()!) + "@";
        }

        if (type.IsGenericType)
        {
            string bare = type.GetGenericTypeDefinition().FullName!;
            bare = bare[..bare.IndexOf('`', StringComparison.Ordinal)];

            return bare + "{" + string.Join(",", type.GetGenericArguments().Select(DocIdOf)) + "}";
        }

        return type.FullName ?? type.Name;
    }

    private static MemberDoc Read(XElement member)
    {
        Dictionary<string, string> parameters = new(StringComparer.Ordinal);

        foreach (XElement parameter in member.Elements("param"))
        {
            string? name = parameter.Attribute("name")?.Value;
            if (name is not null)
            {
                parameters[name] = Flatten(parameter);
            }
        }

        // A node with several outputs carries one returns tag per output, each named. A node with
        // one output carries a single named tag, and that name is the port label.
        Dictionary<string, string> returns = new(StringComparer.Ordinal);
        string? firstReturnName = null;

        foreach (XElement value in member.Elements("returns"))
        {
            string name = value.Attribute("name")?.Value ?? "result";
            returns[name] = Flatten(value);
            firstReturnName ??= name;
        }

        return new MemberDoc
        {
            Summary = Flatten(member.Element("summary")),
            ReturnName = firstReturnName,
            Parameters = parameters,
            ReturnDescriptions = returns,
            Search = (member.Element("search")?.Value ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        };
    }

    /// <summary>
    /// Turns a documentation element into Markdown: <c>see cref</c> and <c>c</c> become inline
    /// code, paragraph breaks survive, and the hard wrapping of the source comment does not.
    /// </summary>
    private static string Flatten(XElement? element)
    {
        if (element is null)
        {
            return string.Empty;
        }

        StringBuilder text = new();

        foreach (XNode node in element.Nodes())
        {
            switch (node)
            {
                case XText raw:
                    text.Append(raw.Value);
                    break;

                case XElement child when child.Name == "see" || child.Name == "c":
                    text.Append('`').Append(Referenced(child)).Append('`');
                    break;

                case XElement child when child.Name == "paramref" || child.Name == "typeparamref":
                    text.Append('`').Append(child.Attribute("name")?.Value ?? string.Empty).Append('`');
                    break;

                case XElement child when child.Name == "b" || child.Name == "i":
                    text.Append("**").Append(child.Value.Trim()).Append("**");
                    break;

                case XElement child:
                    text.Append(child.Value);
                    break;
            }
        }

        return Reflow(text.ToString());
    }

    /// <summary>A cref reads as <c>T:Interlude.Theming.ThemeDefinition</c>; only the last part is useful.</summary>
    private static string Referenced(XElement element)
    {
        string reference = element.Attribute("cref")?.Value ?? element.Value;

        if (reference.Length > 2 && reference[1] == ':')
        {
            reference = reference[2..];
        }

        int parenthesis = reference.IndexOf('(', StringComparison.Ordinal);
        if (parenthesis >= 0)
        {
            reference = reference[..parenthesis];
        }

        // Interlude.Theming.ThemeDefinition -> ThemeDefinition, but Theme.Create stays whole,
        // because a two-part name is how a node is written in a graph.
        string[] parts = reference.Split('.');
        return parts.Length <= 2 ? reference : string.Join('.', parts[^2..]);
    }

    /// <summary>
    /// Unwraps the source comment's hard line breaks while keeping blank-line paragraph breaks.
    /// Doc comments are wrapped to fit a code editor; the browser panel wraps for itself, and
    /// leaving both in place produces a ragged column half the width of the pane.
    /// </summary>
    private static string Reflow(string text)
    {
        string[] paragraphs = Regex.Split(text.Trim(), @"\r?\n[ \t]*\r?\n");

        return string.Join(
            "\n\n",
            paragraphs
                .Select(paragraph => Regex.Replace(paragraph, @"\s*\r?\n\s*", " ").Trim())
                .Where(paragraph => paragraph.Length > 0));
    }
}
