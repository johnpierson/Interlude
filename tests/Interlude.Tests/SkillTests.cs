using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Interlude.Conditions;
using Interlude.Model;
using Interlude.Validation;
using Xunit;

namespace Interlude.Tests;

/// <summary>
/// Keeps the authoring skill's schema reference honest against the assembly.
///
/// <c>skills/interlude-form/reference/schema.md</c> is what tells the skill which elements exist
/// and what properties they take, and a skill cannot write a control it has never heard of. The
/// file is generated — <c>Interlude.Preview.exe --schema skills/interlude-form/reference/schema.md</c>
/// — but it is also checked in, because a reference that only exists at pack time cannot be read
/// in the repository or reviewed in a pull request.
///
/// Which means it can fall behind, and the moment it does the skill starts quietly emitting the
/// subset of the schema that existed when someone last remembered to regenerate. So: add a
/// control, or a property to one, and this fails until the reference knows about it.
/// </summary>
public class SkillTests
{
    private static readonly string SkillRoot = Path.Combine(RepoPaths.Root, "skills", "interlude-form");

    /// <summary>The polymorphic roots. Everything a form file can contain hangs off one of these.</summary>
    private static readonly Type[] Roots =
    {
        typeof(FormElement),
        typeof(ConditionExpr),
        typeof(ComputedValue),
        typeof(ValidationRule),
    };

    private static string Reference()
    {
        string path = Path.Combine(SkillRoot, "reference", "schema.md");

        Assert.True(
            File.Exists(path),
            $"The skill's schema reference is missing from {path}. Generate it with " +
            "Interlude.Preview.exe --schema skills/interlude-form/reference/schema.md");

        return File.ReadAllText(path);
    }

    public static TheoryData<string, string> Discriminators()
    {
        TheoryData<string, string> data = new();

        foreach (Type root in Roots)
        {
            foreach (JsonDerivedTypeAttribute derived in root.GetCustomAttributes<JsonDerivedTypeAttribute>())
            {
                data.Add(root.Name, (string)derived.TypeDiscriminator!);
            }
        }

        return data;
    }

    /// <summary>
    /// Every <c>$type</c> the reader accepts has a section of its own.
    ///
    /// This is the test that fires when a control is added, which is the whole reason the file is
    /// generated rather than written.
    /// </summary>
    [Theory]
    [MemberData(nameof(Discriminators))]
    public void Every_discriminator_is_documented(string root, string discriminator)
    {
        Assert.Contains(
            $"### `{discriminator}`",
            Reference());

        // Named so a failure says which family gained a member, because "textBox is missing" is
        // less useful than knowing an element rather than a rule was added.
        Assert.False(string.IsNullOrEmpty(root));
    }

    public static TheoryData<string, string> Properties()
    {
        TheoryData<string, string> data = new();

        foreach (Type type in SchemaTypes())
        {
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetCustomAttribute<JsonIgnoreAttribute>() is not null ||
                    property.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                data.Add(type.Name, JsonNamingPolicy.CamelCase.ConvertName(property.Name));
            }
        }

        return data;
    }

    /// <summary>
    /// Every property the reader accepts is named somewhere in the reference.
    ///
    /// Deliberately not "in the right section": that would mean parsing the Markdown back into a
    /// model, and a property that has moved between sections is a formatting problem, where a
    /// property that appears nowhere is a skill that will never write it.
    /// </summary>
    [Theory]
    [MemberData(nameof(Properties))]
    public void Every_property_is_documented(string owner, string property)
    {
        Assert.True(
            Reference().Contains($"`{property}`", StringComparison.Ordinal),
            $"{owner}.{property} is in the schema but not in the skill's reference. Regenerate it " +
            "with Interlude.Preview.exe --schema skills/interlude-form/reference/schema.md");
    }

    /// <summary>
    /// The types that make up a form document: the polymorphic roots, everything derived from
    /// them, and the objects their properties point at.
    /// </summary>
    private static IEnumerable<Type> SchemaTypes()
    {
        HashSet<Type> types = new()
        {
            typeof(FormDefinition),
            typeof(FormButtons),
            typeof(WindowOptions),
            typeof(ElementStyle),
            typeof(OptionItem),
            typeof(Theming.ThemeDefinition),
        };

        foreach (Type root in Roots)
        {
            types.Add(root);

            foreach (JsonDerivedTypeAttribute derived in root.GetCustomAttributes<JsonDerivedTypeAttribute>())
            {
                types.Add(derived.DerivedType);
            }
        }

        return types;
    }

    /// <summary>
    /// A skill Claude Code will not load is not a skill. The front matter has to be there, and it
    /// has to carry the two keys that decide whether the skill is ever reached.
    /// </summary>
    [Fact]
    public void The_skill_has_usable_front_matter()
    {
        string path = Path.Combine(SkillRoot, "SKILL.md");
        Assert.True(File.Exists(path), $"There is no SKILL.md at {path}.");

        string[] lines = File.ReadAllLines(path);

        Assert.True(lines.Length > 0 && lines[0].Trim() == "---", "SKILL.md must open with '---'.");

        int end = Array.IndexOf(lines, "---", 1);
        Assert.True(end > 1, "SKILL.md's front matter is not closed.");

        string[] frontMatter = lines[1..end];

        Assert.Contains(frontMatter, line => line.StartsWith("name: interlude-form", StringComparison.Ordinal));
        Assert.Contains(frontMatter, line => line.StartsWith("description: ", StringComparison.Ordinal));
    }

    /// <summary>
    /// The files SKILL.md sends the reader to have to exist, because a skill that follows a broken
    /// link falls back on guessing the schema.
    /// </summary>
    [Theory]
    [InlineData("README.md")]
    [InlineData("reference/schema.md")]
    [InlineData("reference/authoring.md")]
    public void The_skill_is_complete(string file)
    {
        Assert.True(
            File.Exists(Path.Combine(SkillRoot, file.Replace('/', Path.DirectorySeparatorChar))),
            $"skills/interlude-form/{file} is missing.");
    }
}
