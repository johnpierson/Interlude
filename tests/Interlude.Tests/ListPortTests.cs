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

        ("Compute.Lookup", "lookupKeys"),
        ("Compute.Lookup", "lookupValues"),

        ("Behavior.WithValidation", "rule"),
    };

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
