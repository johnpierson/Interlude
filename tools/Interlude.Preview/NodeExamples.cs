using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Interlude.Model;

namespace Interlude.Preview;

/// <summary>
/// Draws the form each node's example graph builds.
///
/// The graphs and these pictures are generated from one file, <c>docs/nodes/examples.spec.json</c>:
/// the graph by <c>scripts/make-node-examples.mjs</c>, the picture here. Both read the same
/// signature and the same literal arguments, and this one reaches them by calling the node itself —
/// so the form on the page is built by the code that will build it at run time, rather than by a
/// second description of the node that can quietly fall out of step with the first.
///
/// The alternative was to run each graph in Dynamo and read the definition off a
/// <c>Form.ToJson</c> node, which is where these came from originally. It works, and it costs a
/// round trip through a running Dynamo per node.
/// </summary>
internal static class NodeExamples
{
    /// <summary>Renders every specced node's form into <paramref name="folder"/>.</summary>
    /// <param name="folder">The node help folder — <c>docs/nodes</c>.</param>
    internal static void CaptureForms(string folder)
    {
        string spec = Path.Combine(folder, "examples.spec.json");

        if (!File.Exists(spec))
        {
            Console.WriteLine("no examples.spec.json in " + folder + "; nothing to draw.");
            return;
        }

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(spec));
        int drawn = 0, skipped = 0;

        foreach (JsonElement entry in document.RootElement.EnumerateArray())
        {
            string node = entry.GetProperty("node").GetString()!;

            try
            {
                FormDefinition definition = Build(entry);
                Screenshots.Capture(definition, Path.Combine(folder, node + "_form.png"));
                drawn++;
            }
            catch (Exception error)
            {
                // One bad entry should not cost the other hundred their pictures, and the name of
                // the node that failed is the whole diagnosis.
                Console.WriteLine("skipped " + node + ": " + Unwrap(error).Message);
                skipped++;
            }
        }

        Console.WriteLine($"drew {drawn} form picture(s) in {folder}"
            + (skipped > 0 ? $", skipped {skipped}" : string.Empty));
    }

    /// <summary>Calls the node the spec names, and puts what it returns in a form.</summary>
    private static FormDefinition Build(JsonElement entry)
    {
        // A node that returns a form element is itself what goes into the form. A condition, a
        // rule or a computed value is an ingredient, and its spec carries a `graph` whose root is
        // whatever consumes it. See the note on the same line in make-node-examples.mjs.
        JsonElement root = entry.TryGetProperty("graph", out JsonElement hosted) ? hosted : entry;

        // A theme and an options bundle are not contents, they are presentation — and a Theme
        // node's page whose picture ignored its theme would be illustrating nothing at all.
        return global::Interlude.Form.Create(
            entry.GetProperty("title").GetString()!,
            new List<object> { Call(root) },
            theme: Side(entry, "theme"),
            options: Side(entry, "options"));
    }

    /// <summary>The theme or options a spec hangs off the form, or null when it names none.</summary>
    private static object? Side(JsonElement entry, string name) =>
        entry.TryGetProperty(name, out JsonElement side) ? Call(side) : null;

    /// <summary>
    /// Invokes one node from its spec entry, building whatever it is made of first.
    ///
    /// Depth first, and recursive: a container is given elements rather than literals, a Behavior
    /// node decorates one that already exists, and a Tabs is made of TabPages that are themselves
    /// made of fields.
    /// </summary>
    private static object Call(JsonElement entry)
    {
        List<object> children = entry.TryGetProperty("children", out JsonElement declared)
            ? declared.EnumerateArray().Select(child => Call(child)).ToList()
            : [];

        MethodInfo node = Resolve(entry.GetProperty("signature").GetString()!);
        JsonElement[] given = entry.GetProperty("args").EnumerateArray().ToArray();

        object?[] arguments = node.GetParameters()
            .Select((parameter, i) => i < given.Length
                ? Argument(given[i], parameter.ParameterType, children)
                : parameter.HasDefaultValue ? parameter.DefaultValue : null)
            .ToArray();

        return node.Invoke(null, arguments)
            ?? throw new InvalidOperationException("the node returned nothing.");
    }

    /// <summary>
    /// One argument: either a reference to what was built upstream, or a plain value.
    ///
    /// The sentinels match the ones the graph generator reads, so the picture and the graph are
    /// wired the same way — <c>$children</c> for all of them, <c>$child0</c> for one.
    /// </summary>
    private static object? Argument(JsonElement value, Type wanted, List<object>? children)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            string text = value.GetString()!;

            if (text == "$children")
            {
                if (children is null or { Count: 0 })
                {
                    throw new InvalidOperationException("$children with no children declared.");
                }

                return TypedList(children, wanted);
            }

            if (text.StartsWith("$child", StringComparison.Ordinal)
                && int.TryParse(text[6..], out int which))
            {
                return which < (children?.Count ?? 0)
                    ? children![which]
                    : throw new InvalidOperationException($"no child {which} declared.");
            }
        }

        // A chosen few of the children, as a list.
        if (value.ValueKind == JsonValueKind.Array
            && value.GetArrayLength() > 0
            && value.EnumerateArray().All(Names))
        {
            return TypedList(
                value.EnumerateArray().Select(item => Argument(item, typeof(object), children)!),
                wanted);
        }

        return Coerce(value, wanted);
    }

    /// <summary>Whether a spec value is a reference to a child rather than a plain value.</summary>
    private static bool Names(JsonElement value) =>
        value.ValueKind == JsonValueKind.String
        && value.GetString()!.StartsWith("$child", StringComparison.Ordinal);

    /// <summary>
    /// Gathers values into a list of the type the port declares.
    ///
    /// Not <c>List&lt;object&gt;</c>: a container takes <c>List&lt;FormElement&gt;</c>, and
    /// reflection will not hand one a list of objects however many form elements are inside it.
    /// </summary>
    private static IList TypedList(IEnumerable<object> values, Type wanted)
    {
        Type item = wanted.IsGenericType ? wanted.GetGenericArguments()[0] : typeof(object);
        IList typed = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(item))!;

        foreach (object value in values)
        {
            typed.Add(value);
        }

        return typed;
    }

    /// <summary>
    /// Finds the method a Dynamo signature names.
    ///
    /// <c>Interlude.Input.Slider@string,double,…</c> splits into the type <c>Interlude.Input</c>,
    /// the method <c>Slider</c>, and the parameter list that tells the overloads apart.
    /// </summary>
    private static MethodInfo Resolve(string signature)
    {
        string[] halves = signature.Split('@');
        string path = halves[0];

        int lastDot = path.LastIndexOf('.');
        string typeName = path[..lastDot];
        string methodName = path[(lastDot + 1)..];

        Type type = typeof(FormDefinition).Assembly.GetType(typeName)
            ?? throw new InvalidOperationException($"no type named {typeName}.");

        MethodInfo[] candidates = type
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == methodName)
            .ToArray();

        if (candidates.Length == 0)
        {
            throw new InvalidOperationException($"{typeName} has no method {methodName}.");
        }

        if (candidates.Length == 1)
        {
            return candidates[0];
        }

        int arity = halves.Length > 1 && halves[1].Length > 0
            ? halves[1].Split(',').Length
            : 0;

        return candidates.FirstOrDefault(m => m.GetParameters().Length == arity)
            ?? throw new InvalidOperationException(
                $"{methodName} has no overload taking {arity} argument(s).");
    }

    /// <summary>Reads a spec value as the type the parameter wants.</summary>
    private static object? Coerce(JsonElement value, Type wanted)
    {
        // null means "leave this one out" — a minimum that is absent, a default the node should
        // choose for itself. It reaches every kind of port, including the ones taking lists.
        if (value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        Type target = Nullable.GetUnderlyingType(wanted) ?? wanted;

        if (target == typeof(string))
        {
            return value.GetString();
        }

        if (target == typeof(double))
        {
            return value.GetDouble();
        }

        if (target == typeof(int))
        {
            return value.GetInt32();
        }

        if (target == typeof(bool))
        {
            return value.GetBoolean();
        }

        if (target == typeof(List<object>) || target == typeof(IList<object>))
        {
            return value.EnumerateArray().Select(Loose).ToList();
        }

        // Ports typed var take whatever the JSON says they are — a minimum that is a number on one
        // node and absent on another is exactly why those ports are var in the first place.
        return Loose(value);
    }

    private static object? Loose(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Number => value.TryGetInt32(out int whole) ? whole : value.GetDouble(),
        JsonValueKind.Array => value.EnumerateArray().Select(Loose).ToList(),
        JsonValueKind.Null => null,
        _ => null,
    };

    private static Exception Unwrap(Exception error) =>
        error is TargetInvocationException { InnerException: { } inner } ? inner : error;
}
