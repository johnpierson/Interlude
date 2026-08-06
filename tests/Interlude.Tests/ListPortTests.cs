using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autodesk.DesignScript.Runtime;
using Xunit;

namespace Interlude.Tests;

/// <summary>
/// Every port that takes a list must be declared as a list.
///
/// This is the rule behind the worst bug the package has shipped, and it is worth stating in full
/// because nothing about it is obvious from C#.
///
/// A zero-touch parameter typed <c>object</c> becomes DesignScript <c>var</c>, which is rank 0 — a
/// single value. Hand a list to a rank-0 port and Dynamo does not pass the list: it *replicates*,
/// calling the node once per item. So <c>Input.ListBox</c> given 22 sheets produced 22 separate
/// list boxes, each holding one sheet, each with its own copy of the label. The node looked
/// plausible, threw nothing, and was wrong.
///
/// Declaring the parameter as a collection makes it <c>var[]</c>, which takes the list whole. It
/// costs nothing at the other end, because DesignScript promotes a lone value into a one-item list
/// on the way in — so a port declared this way still accepts a single item.
///
/// None of the existing tests could catch this. They call the nodes from C#, where a
/// <c>List&lt;object&gt;</c> argument behaves identically either way; the difference only exists
/// inside Dynamo's evaluator. So this test reads the *declarations* instead.
/// </summary>
public class ListPortTests
{
    /// <summary>
    /// Ports that take a list, by node and parameter name.
    ///
    /// Listed explicitly rather than guessed from the name: <c>value</c> on <c>Input.TreeItem</c>
    /// is deliberately a single item, and <c>defaultValue</c> is a list on the multi-select inputs
    /// and a single item on the rest. A wrong guess in either direction would make this test lie.
    /// </summary>
    private static readonly (string Node, string Parameter)[] ListPorts =
    {
        ("Form.Show", "elements"),
        ("Form.Create", "elements"),
        ("Form.Options", "extraButtons"),

        ("Input.DropDown", "items"),
        ("Input.DropDown", "displayNames"),
        ("Input.ListBox", "items"),
        ("Input.ListBox", "displayNames"),
        ("Input.ListBox", "defaultValue"),
        ("Input.RadioButtons", "items"),
        ("Input.RadioButtons", "displayNames"),
        ("Input.TreeSelect", "nodes"),
        ("Input.TreeSelect", "defaultValue"),
        ("Input.TreeItem", "children"),
        ("Input.ColorPicker", "presets"),

        ("Condition.In", "values"),
        ("Condition.And", "conditions"),
        ("Condition.Or", "conditions"),

        ("Compute.Sum", "keys"),
        ("Compute.Lookup", "lookupKeys"),
        ("Compute.Lookup", "lookupValues"),

        ("Behavior.WithValidation", "rule"),
    };

    /// <summary>
    /// Ports that really are a single value, with why.
    ///
    /// This exists so the rule can be enforced from the other direction. The list above is
    /// hand-written, and a hand-written list is exactly how <c>Compute.Sum</c> was missed on the
    /// first pass — it takes a list of field keys and nobody noticed. So instead of trusting that
    /// list to be complete, the test below walks *every* port typed as a bare value and requires it
    /// to appear here. A new one fails until somebody decides which it is, which is the decision
    /// that was skipped the first time.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> KnownScalarPorts =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Form.Show.trigger"] = "a boolean gate; a list would mean nothing",
            ["Form.Show.theme"] = "one theme",
            ["Form.Show.options"] = "one options object",
            ["Form.Create.theme"] = "one theme",
            ["Form.Create.options"] = "one options object",
            ["Form.Options.height"] = "one number, or null for automatic",
            ["Form.ShowDefinition.trigger"] = "a boolean gate",

            ["Input.Number.minimum"] = "one bound, or null",
            ["Input.Number.maximum"] = "one bound, or null",
            ["Input.Integer.minimum"] = "one bound, or null",
            ["Input.Integer.maximum"] = "one bound, or null",
            ["Input.DatePicker.defaultValue"] = "one date",
            ["Input.DatePicker.minimum"] = "one date",
            ["Input.DatePicker.maximum"] = "one date",
            ["Input.DropDown.defaultValue"] = "one selection: the drop-down picks exactly one",
            ["Input.RadioButtons.defaultValue"] = "one selection",
            ["Input.TreeItem.value"] = "what this one item returns",

            ["Condition.Equals.value"] = "one value to compare against",
            ["Condition.NotEquals.value"] = "one value to compare against",
            ["Condition.GreaterThan.value"] = "one value to compare against",
            ["Condition.AtLeast.value"] = "one value to compare against",
            ["Condition.LessThan.value"] = "one value to compare against",
            ["Condition.AtMost.value"] = "one value to compare against",
            ["Condition.Contains.value"] = "one value to look for",
            ["Condition.StartsWith.value"] = "one prefix",
            ["Condition.EndsWith.value"] = "one suffix",

            ["Compute.Arithmetic.left"] = "one operand",
            ["Compute.Arithmetic.right"] = "one operand",
            ["Compute.Constant.value"] = "one value",
            ["Compute.Lookup.fallback"] = "one value",
            ["Compute.If.ifTrue"] = "one operand",
            ["Compute.If.ifFalse"] = "one operand",

            // One template or one computation. A list on this port replicates into one preview
            // per item, which is the useful reading and the one lacing already gives.
            ["Layout.Preview.value"] = "one template, or one computation",

            ["Layout.Image.width"] = "one measurement, or null for the picture's own size",
            ["Layout.Image.height"] = "one measurement, or null for the picture's own size",

            ["Behavior.WithSize.width"] = "one measurement, or null",
            ["Behavior.WithSize.height"] = "one measurement, or null",
            ["Behavior.WithSize.labelWidth"] = "one measurement, or null",
            ["Behavior.WithSize.margin"] = "one measurement, or null",

            ["Rule.Range.minimum"] = "one bound, or null",
            ["Rule.Range.maximum"] = "one bound, or null",
            ["Rule.Length.minimum"] = "one bound, or null",
            ["Rule.Length.maximum"] = "one bound, or null",
            ["Rule.CompareTo.value"] = "one value to compare against",

            ["Result.ValueByKey.result"] = "the answers, which is one dictionary",
            ["Result.GetString.result"] = "the answers",
            ["Result.GetNumber.result"] = "the answers",
            ["Result.GetInteger.result"] = "the answers",
            ["Result.GetBool.result"] = "the answers",
            ["Result.GetDate.result"] = "the answers",
            ["Result.GetColor.result"] = "the answers",
            ["Result.GetList.result"] = "the answers",
            ["Result.GetFilePaths.result"] = "the answers",
            ["Result.Keys.result"] = "the answers",
            ["Result.Values.result"] = "the answers",
            ["Result.HasKey.result"] = "the answers",
            ["Result.WasSubmitted.result"] = "the answers",
            ["Result.WasCancelled.result"] = "the answers",
            ["Result.ButtonClicked.result"] = "the answers",
            ["Result.GetString.fallback"] = "one fallback",
            ["Result.GetNumber.fallback"] = "one fallback",
            ["Result.GetInteger.fallback"] = "one fallback",
            ["Result.GetBool.fallback"] = "one fallback",
            ["Result.GetDate.fallback"] = "one fallback",
            ["Result.ValueByKey.fallback"] = "one fallback",
        };

    /// <summary>
    /// Every bare-value port has been consciously classified as one or the other.
    ///
    /// The first fix for this bug went port by port off a list I wrote by reading the code, and
    /// that list missed <c>Compute.Sum</c>. Enumerating the ports instead of the exceptions is what
    /// makes the rule hold: a newly added <c>object</c> port fails this test until somebody says
    /// which kind it is.
    /// </summary>
    [Fact]
    public void Every_bare_value_port_is_deliberately_a_single_value()
    {
        HashSet<string> declaredLists = ListPorts
            .Select(port => port.Node + "." + port.Parameter)
            .ToHashSet(StringComparer.Ordinal);

        List<string> unclassified = new();

        foreach (MethodInfo node in NodeMethods())
        {
            string nodeName = node.DeclaringType!.Name + "." + node.Name;

            foreach (ParameterInfo port in node.GetParameters())
            {
                if (port.ParameterType != typeof(object))
                {
                    continue;
                }

                string full = nodeName + "." + port.Name;

                if (!KnownScalarPorts.ContainsKey(full) && !declaredLists.Contains(full))
                {
                    unclassified.Add(full);
                }
            }
        }

        Assert.True(
            unclassified.Count == 0,
            "These ports are typed as a bare value and have not been classified. If the port takes " +
            "a list, declare it as one — Dynamo replicates the node once per item otherwise. If it " +
            "genuinely takes a single value, add it to KnownScalarPorts with the reason:\n  " +
            string.Join("\n  ", unclassified));
    }

    private static IEnumerable<MethodInfo> NodeMethods()
        => typeof(Form).Assembly
            .GetExportedTypes()
            .Where(type => type.Namespace == "Interlude" && type.IsClass)
            .SelectMany(type => type.GetMethods(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly));

    [Fact]
    public void Every_port_that_takes_a_list_is_declared_as_a_list()
    {
        List<string> scalars = new();

        foreach ((string node, string parameter) in ListPorts)
        {
            ParameterInfo port = PortOf(node, parameter);

            if (!IsCollection(port.ParameterType))
            {
                scalars.Add($"{node}.{parameter} is {port.ParameterType.Name}");
            }
        }

        Assert.True(
            scalars.Count == 0,
            "These ports take a list but are declared as a single value, so Dynamo will replicate " +
            "the node once per item instead of passing the list:\n  " +
            string.Join("\n  ", scalars));
    }

    /// <summary>
    /// The list above has to name ports that exist. Renaming a parameter without updating it here
    /// would otherwise leave the rule silently unenforced for that port.
    /// </summary>
    [Fact]
    public void Every_listed_port_exists()
    {
        foreach ((string node, string parameter) in ListPorts)
        {
            Assert.NotNull(PortOf(node, parameter));
        }
    }

    /// <summary>
    /// The mirror of the main test: a list-typed port must still accept a single value, which is
    /// what DesignScript's promotion gives us and what stops this fix from breaking the common
    /// case of one rule or one item.
    /// </summary>
    [Fact]
    public void A_list_port_still_accepts_a_single_item()
    {
        Model.FormElement one = Input.ListBox(
            "Sheets",
            new List<object> { "A101" },
            key: "sheets");

        Model.ListSelectionElement list = Assert.IsType<Model.ListSelectionElement>(one);
        Assert.Single(list.Options);
    }

    /// <summary>And the case that started it: many items make one control, not many controls.</summary>
    [Fact]
    public void A_list_port_given_many_items_makes_one_control_holding_them_all()
    {
        List<object> sheets = Enumerable.Range(1, 22).Select(i => (object)$"A{i:000}").ToList();

        Model.FormElement element = Input.ListBox("Sheets", sheets, key: "sheets");
        Model.ListSelectionElement list = Assert.IsType<Model.ListSelectionElement>(element);

        Assert.Equal(22, list.Options.Count);
        Assert.Equal("A001", list.Options[0].Display);
    }

    private static ParameterInfo PortOf(string node, string parameter)
    {
        string[] parts = node.Split('.');
        Type type = typeof(Form).Assembly.GetType("Interlude." + parts[0])
            ?? throw new InvalidOperationException($"No node class named {parts[0]}.");

        MethodInfo method = type.GetMethod(parts[1], BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException($"No node named {node}.");

        return method.GetParameters().FirstOrDefault(p => p.Name == parameter)
            ?? throw new InvalidOperationException($"{node} has no port named '{parameter}'.");
    }

    /// <summary>
    /// What DesignScript sees as <c>var[]</c>. A string is a collection to the CLR and a single
    /// value to everybody else, so it does not count.
    /// </summary>
    private static bool IsCollection(Type type)
        => type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);
}
