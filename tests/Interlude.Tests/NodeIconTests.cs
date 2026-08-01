using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using Xunit;

namespace Interlude.Tests;

/// <summary>
/// Every node has an icon, and the file that carries them is the shape Dynamo expects.
///
/// None of this is checked by the compiler, and none of it fails loudly at runtime. Dynamo looks up
/// an icon by a string built from the node's fully qualified name; a miss is not an error, it is
/// the default cube. So a renamed node, a new node, or a resource stream named a shade differently
/// all produce the same silent symptom, in a place nobody looks until a user mentions it.
///
/// The generator in the preview harness checks its catalogue against the assembly, but the
/// generator only runs when somebody remembers to run it. This suite runs on every build, and it
/// reads the checked-in artefacts rather than the catalogue, so it fails on the case that actually
/// matters: what is committed no longer matches the nodes that exist.
/// </summary>
public class NodeIconTests
{
    /// <summary>The two sizes Dynamo asks for, by the suffix it appends to the icon name.</summary>
    private static readonly string[] Sizes = { "Small", "Large" };

    private static string IconProject => Path.Combine(RepoPaths.Root, "src", "Interlude.Icons");

    /// <summary>
    /// The name Dynamo derives for the resource stream: the node library's assembly name with
    /// "Images" on the end. Interlude.dll therefore means InterludeImages.resources, and this is
    /// the string the whole feature hangs on.
    /// </summary>
    private const string StreamName = "InterludeImages.resources";

    private static IReadOnlyList<string> Keys { get; } = ReadKeys();

    [Fact]
    public void Every_node_has_an_icon_at_both_sizes()
    {
        List<string> missing = new();

        foreach (string node in NodeNames())
        {
            foreach (string size in Sizes)
            {
                string key = $"Interlude.{node}.{size}";

                if (!Keys.Contains(key, StringComparer.Ordinal))
                {
                    missing.Add(key);
                }
            }
        }

        Assert.True(
            missing.Count == 0,
            "These nodes have no icon, so Dynamo will draw them with its default cube. Add them to " +
            "the catalogue in tools/Interlude.Preview/Icons.cs and regenerate:\n  " +
            string.Join("\n  ", missing));
    }

    /// <summary>
    /// The mirror: an icon for a node that no longer exists is dead weight nobody will ever notice,
    /// because the symptom of a stale icon is nothing at all.
    /// </summary>
    [Fact]
    public void No_icon_belongs_to_a_node_that_does_not_exist()
    {
        HashSet<string> expected = NodeNames()
            .SelectMany(node => Sizes.Select(size => $"Interlude.{node}.{size}"))
            .ToHashSet(StringComparer.Ordinal);

        string[] orphans = Keys.Where(key => !expected.Contains(key)).OrderBy(k => k, StringComparer.Ordinal).ToArray();

        Assert.True(
            orphans.Length == 0,
            "These icons do not belong to any node:\n  " + string.Join("\n  ", orphans));
    }

    /// <summary>
    /// The PNG behind every key has to actually be there and be the right size, because a resource
    /// entry holding nothing is indistinguishable from a healthy one until Dynamo tries to draw it.
    /// </summary>
    [Fact]
    public void Every_icon_is_a_png_of_the_size_its_name_claims()
    {
        Dictionary<string, int> expectedPixels = new(StringComparer.Ordinal)
        {
            ["Small"] = 32,
            ["Large"] = 128,
        };

        List<string> wrong = new();

        foreach ((string key, byte[] bytes) in ReadEntries())
        {
            string size = key[(key.LastIndexOf('.') + 1)..];

            if (bytes.Length < 24 || bytes[0] != 0x89 || bytes[1] != 'P' || bytes[2] != 'N' || bytes[3] != 'G')
            {
                wrong.Add($"{key} is not a PNG");
                continue;
            }

            // Width and height are the first two big-endian integers of the IHDR chunk, which the
            // format requires to come first.
            int width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
            int height = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];

            if (width != expectedPixels[size] || height != expectedPixels[size])
            {
                wrong.Add($"{key} is {width}x{height}, expected {expectedPixels[size]} square");
            }
        }

        Assert.True(wrong.Count == 0, string.Join("\n  ", wrong));
    }

    /// <summary>
    /// The container must be readable by the plain <see cref="ResourceReader"/>.
    ///
    /// This is the check that caught the format the icons were nearly shipped in. Building the
    /// container from a .resx forces MSBuild's preserialized writer, which stamps a header naming
    /// System.Resources.Extensions.DeserializingResourceReader; ResourceReader throws on that
    /// header rather than ignoring it, and Dynamo's own customization assemblies are all written
    /// for ResourceReader. The icons would have been silently absent in the only host that matters.
    /// </summary>
    [Fact]
    public void The_container_is_readable_the_way_Dynamos_own_icon_assemblies_are()
    {
        Assert.NotEmpty(Keys);
        Assert.Equal(Keys.Count, Keys.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// Overloads would need the parameter types in the icon name, and nothing here does that.
    ///
    /// Dynamo disambiguates overloaded nodes by appending their DesignScript parameter types to the
    /// icon name — <c>DSCore.Math.Sign.int.Small</c> in its own assembly. Interlude has no
    /// overloaded nodes, so the generator does not implement it; the moment one is added, its icon
    /// would go looking under a name that is not there.
    /// </summary>
    [Fact]
    public void No_node_is_overloaded()
    {
        string[] overloaded = NodeMethods()
            .GroupBy(method => method.DeclaringType!.Name + "." + method.Name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            overloaded.Length == 0,
            "These nodes are overloaded, which changes the name Dynamo looks their icon up under. " +
            "Either give them distinct names or teach the generator to append the parameter " +
            "types:\n  " + string.Join("\n  ", overloaded));
    }

    /// <summary>The container ships inside the assembly under exactly this name, or not at all.</summary>
    [Fact]
    public void The_resource_container_is_checked_in_where_the_project_expects_it()
    {
        string container = Path.Combine(IconProject, StreamName);

        Assert.True(
            File.Exists(container),
            $"{container} is missing. It is generated with: Interlude.Preview.exe --icons src/Interlude.Icons");

        string project = File.ReadAllText(Path.Combine(IconProject, "Interlude.Icons.csproj"));

        Assert.Contains($"LogicalName=\"{StreamName}\"", project, StringComparison.Ordinal);
        Assert.Contains("<AssemblyName>Interlude.customization</AssemblyName>", project, StringComparison.Ordinal);
    }

    /// <summary>Both PNGs behind every key are on disk too, since those are what a reviewer looks at.</summary>
    [Fact]
    public void The_images_are_checked_in_beside_the_container()
    {
        string images = Path.Combine(IconProject, "Images");

        HashSet<string> onDisk = Directory
            .GetFiles(images, "Interlude.*.png")
            .Select(Path.GetFileNameWithoutExtension)
            .ToHashSet(StringComparer.Ordinal)!;

        string[] missing = Keys.Where(key => !onDisk.Contains(key)).OrderBy(k => k, StringComparer.Ordinal).ToArray();
        string[] extra = onDisk.Where(name => !Keys.Contains(name, StringComparer.Ordinal))
            .OrderBy(n => n, StringComparer.Ordinal).ToArray();

        Assert.True(missing.Length == 0, "In the container but not on disk:\n  " + string.Join("\n  ", missing));
        Assert.True(extra.Length == 0, "On disk but not in the container:\n  " + string.Join("\n  ", extra));
    }

    private static IReadOnlyList<string> ReadKeys()
        => ReadEntries().Select(entry => entry.Key).ToArray();

    private static IEnumerable<(string Key, byte[] Bytes)> ReadEntries()
    {
        string container = Path.Combine(IconProject, StreamName);

        if (!File.Exists(container))
        {
            // Reported properly by the test that checks for it; returning empty here keeps the
            // other failures readable instead of burying them in the same IO exception.
            yield break;
        }

        using FileStream file = File.OpenRead(container);
        using ResourceReader reader = new(file);

        foreach (DictionaryEntry entry in reader)
        {
            yield return ((string)entry.Key, (byte[])entry.Value!);
        }
    }

    private static IEnumerable<string> NodeNames()
        => NodeMethods().Select(method => method.DeclaringType!.Name + "." + method.Name).Distinct(StringComparer.Ordinal);

    private static IEnumerable<MethodInfo> NodeMethods()
        => typeof(Form).Assembly
            .GetExportedTypes()
            .Where(type => type.Namespace == "Interlude" && type.IsClass)
            .SelectMany(type => type.GetMethods(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly));
}
