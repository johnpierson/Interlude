using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Interlude.Tests;

/// <summary>
/// Guards against the failure mode that takes the whole package down at once.
///
/// Dynamo imports a zero-touch assembly by reflecting over **every public type in it** and
/// building a DesignScript AST for every public constructor and method. That happens regardless
/// of <c>[IsVisibleInDynamoLibrary(false)]</c> — the attribute controls what appears in the
/// library, not what gets imported. So one type Dynamo's importer cannot parse does not hide one
/// node: it throws <c>LibraryLoadFailedException</c> and **not a single Interlude node loads**.
///
/// It has happened once. <c>RgbColor</c> had a constructor whose <c>alpha</c> parameter defaulted
/// to <c>255</c>, a <see cref="byte"/>. Dynamo's <c>AstFactory.BuildPrimitiveNodeFromObject</c>
/// has no case for <see cref="byte"/>, so it asserted "Invalid Input type to make AST node" and
/// the package appeared completely empty in Dynamo 4.1 with no clue why.
///
/// These tests encode the importer's rules so the next one fails here instead — in a build,
/// with the offending member named — rather than in someone's Revit session.
/// </summary>
public class ZeroTouchImportTests
{
    /// <summary>
    /// The types <c>ProtoCore.AST.AssociativeAST.AstFactory.BuildPrimitiveNodeFromObject</c>
    /// knows how to turn into a literal. Anything else asserts and fails the import.
    ///
    /// Deliberately conservative: this list is what Dynamo actually handles, not what it might
    /// plausibly handle. Widening it means having checked the importer's source.
    /// </summary>
    private static readonly HashSet<Type> AstLiteralTypes = new()
    {
        typeof(bool),
        typeof(char),
        typeof(string),
        typeof(int),
        typeof(long),
        typeof(double),
        typeof(float),
    };

    /// <summary>
    /// Every optional parameter on the public surface must have a default Dynamo can render as a
    /// literal. This is the exact check that would have caught the RgbColor regression.
    /// </summary>
    [Fact]
    public void Every_optional_parameter_has_a_default_Dynamo_can_import()
    {
        List<string> offenders = new();

        foreach (MethodBase member in PublicMembers())
        {
            foreach (ParameterInfo parameter in member.GetParameters())
            {
                if (!parameter.HasDefaultValue)
                {
                    continue;
                }

                object? value = parameter.DefaultValue;

                // A null default is fine: Dynamo builds a null node for it.
                if (value is null)
                {
                    continue;
                }

                Type valueType = value.GetType();

                // An enum default arrives boxed as its underlying type, which is usually int —
                // but a byte- or short-backed enum would not be importable.
                if (valueType.IsEnum)
                {
                    valueType = Enum.GetUnderlyingType(valueType);
                }

                if (!AstLiteralTypes.Contains(valueType))
                {
                    offenders.Add(
                        $"{Describe(member)} parameter '{parameter.Name}' defaults to a " +
                        $"{valueType.Name} ({value}), which Dynamo's AstFactory cannot import");
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            "These members would stop the whole assembly loading in Dynamo:\n  " +
            string.Join("\n  ", offenders));
    }

    /// <summary>
    /// The same rule stated where it is easiest to break it by accident. A public constructor is
    /// imported as a node whether or not anyone wanted one, so its signature is Dynamo's problem
    /// too.
    /// </summary>
    [Fact]
    public void No_public_constructor_takes_a_parameter_Dynamo_cannot_represent()
    {
        List<string> offenders = new();

        foreach (Type type in PublicTypes())
        {
            foreach (ConstructorInfo constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                foreach (ParameterInfo parameter in constructor.GetParameters())
                {
                    // Only defaults are parsed as literals; the parameter's own type is marshalled
                    // and may be anything.
                    if (parameter.HasDefaultValue &&
                        parameter.DefaultValue is not null &&
                        !AstLiteralTypes.Contains(UnderlyingOf(parameter.DefaultValue.GetType())))
                    {
                        offenders.Add($"{type.Name}..ctor parameter '{parameter.Name}'");
                    }
                }
            }
        }

        Assert.True(offenders.Count == 0, "Unimportable constructors: " + string.Join(", ", offenders));
    }

    /// <summary>
    /// Interlude's own value types are the ones most likely to trip this, because they are the
    /// ones with hand-written constructors. Their byte members are what the regression was about.
    /// </summary>
    [Fact]
    public void The_colour_type_can_still_be_constructed_from_code()
    {
        // Regression guard with teeth: the fix must not have removed the ability to build a
        // colour, only the unimportable default.
        Model.RgbColor opaque = new(0x33, 0x66, 0xCC);
        Model.RgbColor translucent = new(0x33, 0x66, 0xCC, 0x80);

        Assert.Equal(255, opaque.Alpha);
        Assert.Equal(0x80, translucent.Alpha);
        Assert.Equal("#3366CC", opaque.ToHex());
    }

    /// <summary>
    /// Runs Dynamo's actual importer over the built assembly.
    ///
    /// The two tests above encode what the importer is believed to do. This one asks it. If the
    /// belief is ever wrong — a rule missed, a new Dynamo release tightening something — this is
    /// what notices, and it fails with the same message a user would have seen in Dynamo's
    /// notification panel.
    /// </summary>
    [Fact]
    public void Dynamo_can_import_the_whole_assembly()
    {
        Assembly shipped = typeof(Model.FormDefinition).Assembly;

        ProtoFFI.CLRDLLModule module = new(Path.GetFileName(shipped.Location), shipped);

        // A null type name with an empty alias is how Dynamo imports a whole DLL: every public
        // type in it gets parsed into a DesignScript class.
        Exception? failure = Record.Exception(() => module.ImportCodeBlock(null, string.Empty, null));

        Assert.True(
            failure is null,
            "Dynamo's importer rejected the assembly, which means NO Interlude nodes would load:\n  " +
            Flatten(failure));
    }

    /// <summary>
    /// The library shows one category, not two.
    ///
    /// Dynamo imports a type's base classes and everything in its public signatures, so a public
    /// class deriving from <c>InvalidOperationException</c> puts <c>Exception</c>,
    /// <c>SystemException</c> and <c>InvalidOperationException</c> in the library, and a public
    /// method returning a <c>FrameworkElement</c> puts <c>System.Windows</c> there. The result is
    /// a "System" category sitting next to "Interlude", full of framework types nobody asked for.
    ///
    /// <c>[IsVisibleInDynamoLibrary(false)]</c> cannot fix that: it hides *our* type, not the
    /// framework type behind it. The only fix is to keep those types out of the public surface,
    /// which is why the rendering layer, the exceptions and the live-state types are internal.
    ///
    /// This reads what Dynamo's importer actually produced rather than guessing, and allows only
    /// the handful of framework types that any assembly unavoidably mentions.
    /// </summary>
    [Fact]
    public void Importing_the_assembly_creates_no_category_but_Interlude()
    {
        Assembly shipped = typeof(Model.FormDefinition).Assembly;
        ProtoFFI.CLRDLLModule module = new(Path.GetFileName(shipped.Location), shipped);

        ProtoCore.AST.AssociativeAST.CodeBlockNode block =
            (ProtoCore.AST.AssociativeAST.CodeBlockNode)module.ImportCodeBlock(null, string.Empty, null);

        List<string> foreign = block.Body
            .OfType<ProtoCore.AST.AssociativeAST.ClassDeclNode>()
            .Select(declaration => declaration.ClassName)
            .Where(name => !name.StartsWith("Interlude", StringComparison.Ordinal))
            .Where(name => !IsUnavoidable(name))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            foreign.Count == 0,
            "These framework types would appear in Dynamo's library beside Interlude:\n  " +
            string.Join("\n  ", foreign));
    }

    /// <summary>
    /// Framework types that cannot be kept out and do not produce a stray category anyway.
    ///
    /// <c>Object</c> and <c>ValueType</c> are every type's ancestors. <c>DateTime</c> and
    /// <c>TimeSpan</c> are types Dynamo already knows and maps to its own. Interfaces and
    /// delegates — <c>IEquatable&lt;T&gt;</c> from every record, <c>Func&lt;T&gt;</c>,
    /// <c>EventHandler&lt;T&gt;</c> — are imported but have nothing callable, so the library
    /// makes no entry for them.
    /// </summary>
    private static bool IsUnavoidable(string name)
        => name is "System.Object" or "System.ValueType" or "System.DateTime" or "System.TimeSpan"
           || name.StartsWith("System.IEquatableOf", StringComparison.Ordinal)
           || name.StartsWith("System.FuncOf", StringComparison.Ordinal)
           || name.StartsWith("System.ActionOf", StringComparison.Ordinal)
           || name.StartsWith("System.EventHandlerOf", StringComparison.Ordinal);

    private static string Flatten(Exception? error)
    {
        List<string> lines = new();

        for (Exception? current = error; current is not null; current = current.InnerException)
        {
            lines.Add($"{current.GetType().Name}: {current.Message}");
        }

        return string.Join("\n  ", lines);
    }

    private static Type UnderlyingOf(Type type)
        => type.IsEnum ? Enum.GetUnderlyingType(type) : type;

    private static IEnumerable<Type> PublicTypes()
        => typeof(Model.FormDefinition).Assembly.GetExportedTypes();

    /// <summary>Every constructor and method Dynamo's importer will look at.</summary>
    private static IEnumerable<MethodBase> PublicMembers()
    {
        foreach (Type type in PublicTypes())
        {
            foreach (ConstructorInfo constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                yield return constructor;
            }

            foreach (MethodInfo method in type.GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                yield return method;
            }
        }
    }

    private static string Describe(MethodBase member)
        => $"{member.DeclaringType?.Name}.{(member is ConstructorInfo ? "ctor" : member.Name)}";
}
