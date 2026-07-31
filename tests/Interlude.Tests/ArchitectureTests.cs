using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Autodesk.DesignScript.Runtime;
using Xunit;

namespace Interlude.Tests;

/// <summary>
/// Enforces the layering the package promises.
///
/// Interlude ships exactly one assembly, so layering cannot be enforced by project references.
/// These tests are the enforcement instead. That is a deliberate trade: every extra assembly
/// would multiply across three Dynamo builds and every package folder, and a wrong reference
/// caught by a test is cheaper than a second DLL shipped to every user for ever.
/// </summary>
public class ArchitectureTests
{
    /// <summary>Namespaces that must stay free of WPF, so the core is testable without a UI thread.</summary>
    private static readonly string[] CoreFolders =
    {
        "Model",
        "Conditions",
        "Validation",
        "Runtime",
        "Serialization",
        "Theming",
    };

    /// <summary>
    /// The layering rule, checked against the source rather than the metadata: a method body that
    /// quietly reaches for a Brush would pass a signature check but break headless execution.
    /// </summary>
    [Fact]
    public void The_core_layers_never_reference_WPF()
    {
        List<string> offenders = new();

        foreach (string folder in CoreFolders)
        {
            string path = Path.Combine(RepoPaths.SourceRoot, folder);
            if (!Directory.Exists(path))
            {
                continue;
            }

            foreach (string file in Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories))
            {
                // Comments are stripped first: these files explain at length *why* they avoid
                // System.Windows.Media.Color, and a test that cannot tell prose from code would
                // punish the documentation.
                string source = StripComments(File.ReadAllText(file));

                if (source.Contains("System.Windows", StringComparison.Ordinal))
                {
                    offenders.Add(Path.GetRelativePath(RepoPaths.Root, file));
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            "These files are in a WPF-free layer but reference System.Windows: " +
            string.Join(", ", offenders));
    }

    /// <summary>The renderer contract itself must stay free of WPF, or a second renderer is impossible.</summary>
    [Fact]
    public void The_renderer_contract_never_references_WPF()
    {
        string contract = Path.Combine(RepoPaths.SourceRoot, "Rendering", "IFormRenderer.cs");

        Assert.True(File.Exists(contract), $"Expected the renderer contract at {contract}.");
        Assert.DoesNotContain("System.Windows", StripComments(File.ReadAllText(contract)), StringComparison.Ordinal);
    }

    /// <summary>
    /// Removes comments and string literals so the layering check reads code rather than prose.
    /// Good enough for this job: it does not need to handle every escape sequence, only to stop
    /// a doc comment mentioning a type from being read as a reference to it.
    /// </summary>
    private static string StripComments(string source)
    {
        System.Text.StringBuilder code = new(source.Length);
        int index = 0;

        while (index < source.Length)
        {
            char current = source[index];

            if (current == '/' && index + 1 < source.Length && source[index + 1] == '/')
            {
                while (index < source.Length && source[index] != '\n')
                {
                    index++;
                }

                continue;
            }

            if (current == '/' && index + 1 < source.Length && source[index + 1] == '*')
            {
                int close = source.IndexOf("*/", index + 2, StringComparison.Ordinal);
                index = close < 0 ? source.Length : close + 2;
                continue;
            }

            if (current == '"')
            {
                index++;
                while (index < source.Length && source[index] != '"')
                {
                    index += source[index] == '\\' ? 2 : 1;
                }

                index++;
                continue;
            }

            code.Append(current);
            index++;
        }

        return code.ToString();
    }

    /// <summary>
    /// Only the node facades belong in the Dynamo library. Everything else — the model, the
    /// runtime, the renderer — is public because it crosses ports or extension points, not
    /// because a graph author should see a node for it.
    /// </summary>
    [Fact]
    public void Every_public_type_outside_the_node_namespace_is_hidden_from_the_library()
    {
        List<string> visible = typeof(Model.FormDefinition).Assembly
            .GetExportedTypes()
            .Where(type => type.Namespace != "Interlude")
            .Where(type => !IsHiddenFromLibrary(type))
            .Select(type => type.FullName!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            visible.Count == 0,
            "These types would appear in the Dynamo library. Add [IsVisibleInDynamoLibrary(false)]: " +
            string.Join(", ", visible));
    }

    /// <summary>The node facades are the library, so they must not be hidden from it.</summary>
    [Fact]
    public void Every_node_facade_is_visible_in_the_library()
    {
        List<Type> facades = NodeFacades().ToList();

        Assert.NotEmpty(facades);
        Assert.All(facades, facade => Assert.False(
            IsHiddenFromLibrary(facade),
            $"{facade.Name} is a node facade but is hidden from the library."));
    }

    /// <summary>
    /// Facades are namespaces of functions, not objects. A public constructor would put a
    /// meaningless "Input.Input" creation node in the library.
    /// </summary>
    [Fact]
    public void Node_facades_expose_only_static_methods_and_cannot_be_constructed()
    {
        foreach (Type facade in NodeFacades())
        {
            Assert.All(
                facade.GetConstructors(BindingFlags.Public | BindingFlags.Instance),
                constructor => Assert.Fail($"{facade.Name} has a public constructor."));

            IEnumerable<MemberInfo> instanceMembers = facade
                .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            Assert.All(instanceMembers, member => Assert.Fail(
                $"{facade.Name}.{member.Name} is an instance member; node facades expose only static methods."));
        }
    }

    /// <summary>
    /// The zero-dependency promise. Anything beyond the framework and the compile-time Dynamo
    /// attributes would have to be copied into every package folder, next to every other add-in.
    /// </summary>
    [Fact]
    public void The_shipped_assembly_references_nothing_but_the_framework_and_Dynamo_attributes()
    {
        string[] allowedPrefixes =
        {
            "System",
            "mscorlib",
            "netstandard",

            // In the box on net8.0-windows and net10.0-windows via Microsoft.WindowsDesktop.App.
            "WindowsBase",
            "PresentationCore",
            "PresentationFramework",
            "Microsoft.Win32",

            // Compile-time only: ExcludeAssets="runtime" keeps these out of the package folder,
            // and Dynamo supplies its own copies at load time.
            "DynamoServices",
            "ProtoGeometry",
        };

        List<string> unexpected = typeof(Model.FormDefinition).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => !allowedPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            unexpected.Count == 0,
            "Interlude must ship with no runtime dependencies, but it references: " +
            string.Join(", ", unexpected));
    }

    /// <summary>
    /// Every control the model can describe must have a renderer, or it would silently render as
    /// a placeholder — a regression that no other test would notice.
    /// </summary>
    [Fact]
    public void Every_element_type_has_a_registered_renderer()
    {
        IReadOnlyCollection<Type> registered = Rendering.Wpf.ControlRendererRegistry
            .CreateDefault()
            .RegisteredTypes;

        List<string> missing = typeof(Model.FormElement).Assembly
            .GetTypes()
            .Where(type => typeof(Model.FormElement).IsAssignableFrom(type))
            .Where(type => !type.IsAbstract)
            .Where(type => !registered.Contains(type))
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            missing.Count == 0,
            "These element types have no renderer and would draw as placeholders: " +
            string.Join(", ", missing));
    }

    /// <summary>
    /// Every concrete element must be serializable, or saving a form would fail on whichever
    /// control was added last.
    /// </summary>
    [Fact]
    public void Every_element_type_has_a_JSON_discriminator()
    {
        HashSet<Type> declared = typeof(Model.FormElement)
            .GetCustomAttributes<System.Text.Json.Serialization.JsonDerivedTypeAttribute>()
            .Select(attribute => attribute.DerivedType)
            .ToHashSet();

        List<string> missing = typeof(Model.FormElement).Assembly
            .GetTypes()
            .Where(type => typeof(Model.FormElement).IsAssignableFrom(type))
            .Where(type => !type.IsAbstract && type.IsPublic)
            .Where(type => !declared.Contains(type))
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            missing.Count == 0,
            "These element types cannot be serialized. Add a [JsonDerivedType] to FormElement: " +
            string.Join(", ", missing));
    }

    private static IEnumerable<Type> NodeFacades()
        => typeof(Model.FormDefinition).Assembly
            .GetExportedTypes()
            .Where(type => type.Namespace == "Interlude" && type.IsClass);

    private static bool IsHiddenFromLibrary(Type type)
    {
        IsVisibleInDynamoLibraryAttribute? attribute =
            type.GetCustomAttribute<IsVisibleInDynamoLibraryAttribute>();

        return attribute is not null && !attribute.Visible;
    }
}
