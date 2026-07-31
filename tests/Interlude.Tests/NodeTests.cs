using System;
using System.Collections.Generic;
using System.Linq;
using Interlude.Conditions;
using Interlude.Model;
using Interlude.Runtime;
using Interlude.Validation;
using Xunit;

namespace Interlude.Tests;

/// <summary>
/// Exercises the node facades the way a graph does.
///
/// None of these open a window. <c>Form.Show</c> with its trigger closed is the one path through
/// the node that never needs a dispatcher, and it is also the path most likely to be quietly
/// broken by a change to caching or key resolution.
/// </summary>
public class NodeTests
{
    private static List<object> Elements(params FormElement[] elements) => elements.Cast<object>().ToList();

    [Fact]
    public void Input_nodes_derive_keys_from_their_labels()
    {
        FormDefinition form = Form.Create("Test", Elements(
            Input.TextBox("Wall Type"),
            Input.Number("Height (mm)"),
            Input.CheckBox("Include rooms")));

        Assert.Equal(
            new[] { "wall_type", "height_mm", "include_rooms" },
            form.Inputs().Select(input => input.Key));
    }

    [Fact]
    public void An_explicit_key_wins_over_the_label()
    {
        FormDefinition form = Form.Create("Test", Elements(Input.TextBox("Wall Type", key: "wallType")));

        Assert.Equal("wallType", form.Inputs().Single().Key);
    }

    [Fact]
    public void A_dropdown_returns_the_object_that_was_put_in_rather_than_its_display_name()
    {
        Uri first = new("https://example.com/a");
        Uri second = new("https://example.com/b");

        FormElement dropdown = Input.DropDown(
            "Target",
            new List<object> { first, second },
            new List<object> { "The first one", "The second one" });

        FormDefinition form = Form.Create("Test", Elements(dropdown));
        FormSession session = new(form);

        Assert.Same(first, session.GetValue("target"));

        session.SetValue("target", second);
        Assert.Same(second, session.GetValue("target"));
    }

    [Fact]
    public void Mismatched_display_name_lists_still_produce_a_usable_form()
    {
        FormElement dropdown = Input.DropDown(
            "Choice",
            new List<object> { "a", "b", "c" },
            new List<object> { "Alpha" });

        DropdownElement element = Assert.IsType<DropdownElement>(dropdown);

        Assert.Equal(new[] { "Alpha", "b", "c" }, element.Options.Select(option => option.Display));
    }

    [Fact]
    public void Behavior_nodes_return_a_new_element_and_leave_the_original_alone()
    {
        FormElement original = Input.TextBox("Name");
        FormElement required = Behavior.Required(original);

        Assert.NotSame(original, required);
        Assert.Null(original.RequiredIf);
        Assert.NotNull(required.RequiredIf);
    }

    [Fact]
    public void Behavior_nodes_preserve_the_concrete_element_type()
    {
        FormElement slider = Input.Slider("Ratio");
        FormElement conditional = Behavior.VisibleIf(slider, Condition.IsChecked("flag"));

        Assert.IsType<SliderElement>(conditional);
    }

    [Fact]
    public void WithComputed_refuses_an_element_that_cannot_hold_a_value()
    {
        FormElement label = Layout.Label("Just text");

        ArgumentException error = Assert.Throws<ArgumentException>(
            () => Behavior.WithComputed(label, Compute.Constant(1)));

        Assert.Contains("LabelElement", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Behavior_nodes_explain_an_unconnected_element_port()
    {
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(
            () => Behavior.Required(null!));

        Assert.Contains("element port is connected", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Layout_containers_keep_their_children_in_order()
    {
        FormElement column = Layout.Column(new List<FormElement>
        {
            Input.TextBox("First"),
            Input.TextBox("Second"),
        });

        ContainerElement container = Assert.IsType<VStackElement>(column);
        Assert.Equal(2, container.Children.Count);
    }

    [Fact]
    public void Layout_containers_ignore_an_unconnected_element_in_the_list()
    {
        FormElement column = Layout.Column(new List<FormElement> { Input.TextBox("First"), null!, });

        Assert.Single(Assert.IsType<VStackElement>(column).Children);
    }

    [Fact]
    public void Grid_columns_are_parsed_from_the_compact_syntax()
    {
        GridElement grid = Assert.IsType<GridElement>(
            Layout.Grid(new List<FormElement> { Layout.Label("x") }, "auto, *, 2*, 120"));

        Assert.Equal(4, grid.Columns.Count);
        Assert.Equal(GridTrackKind.Auto, grid.Columns[0].Kind);
        Assert.Equal(GridTrackKind.Star, grid.Columns[1].Kind);
        Assert.Equal(2d, grid.Columns[2].Value);
        Assert.Equal(120d, grid.Columns[3].Value);
    }

    [Fact]
    public void Form_Create_flattens_a_nested_element_list()
    {
        List<object> nested = new()
        {
            Input.TextBox("One"),
            new List<object> { Input.TextBox("Two"), Input.TextBox("Three") },
        };

        FormDefinition form = Form.Create("Test", nested);

        Assert.Equal(3, form.Elements.Count);
    }

    [Fact]
    public void Compute_treats_a_bare_string_operand_as_a_field_reference()
    {
        FormElement total = Behavior.WithComputed(
            Input.Number("Total", key: "total"),
            Compute.Arithmetic("qty", "Multiply", "price"));

        FormSession session = new(Form.Create("Test", Elements(
            Input.Number("Qty", 3, key: "qty"),
            Input.Number("Price", 5, key: "price"),
            total)));

        Assert.Equal(15d, session.GetValue("total"));
    }

    [Fact]
    public void Compute_Format_fills_in_field_values()
    {
        FormElement summary = Behavior.WithComputed(
            Input.TextBox("Summary", key: "summary"),
            Compute.Format("{first} {last}"));

        FormSession session = new(Form.Create("Test", Elements(
            Input.TextBox("First", "Ada", key: "first"),
            Input.TextBox("Last", "Lovelace", key: "last"),
            summary)));

        Assert.Equal("Ada Lovelace", session.GetValue("summary"));
    }

    [Fact]
    public void Compute_Lookup_maps_an_answer_through_a_table()
    {
        FormElement code = Behavior.WithComputed(
            Input.TextBox("Code", key: "code"),
            Compute.Lookup(
                "material",
                new List<object> { "Concrete", "Steel" },
                new List<object> { "CON", "STL" },
                "???"));

        FormSession session = new(Form.Create("Test", Elements(
            Input.DropDown("Material", new List<object> { "Concrete", "Steel" }, key: "material"),
            code)));

        Assert.Equal("CON", session.GetValue("code"));

        session.SetValue("material", "Steel");
        Assert.Equal("STL", session.GetValue("code"));
    }

    [Fact]
    public void Rules_attached_by_node_are_enforced_by_the_session()
    {
        FormElement age = Behavior.WithValidation(
            Input.Number("Age", key: "age"),
            Rule.Range(18, 120, "Must be an adult."));

        FormSession session = new(Form.Create("Test", Elements(age)));

        session.SetValue("age", 10d);
        Assert.Equal("Must be an adult.", session.GetState("age")!.Error);

        session.SetValue("age", 42d);
        Assert.True(session.GetState("age")!.IsValid);
    }

    [Fact]
    public void Several_rules_can_be_attached_to_one_element()
    {
        FormElement code = Behavior.WithValidation(
            Input.TextBox("Code", key: "code"),
            new List<object> { Rule.Length(3, 3), Rule.Regex("^[A-Z]+$") });

        Assert.Equal(2, code.Rules.Count);
    }

    [Fact]
    public void Form_Check_reports_a_condition_that_names_a_field_that_does_not_exist()
    {
        FormDefinition form = Form.Create("Test", Elements(
            Behavior.VisibleIf(Input.TextBox("Reason"), Condition.IsChecked("nosuchfield"))));

        Dictionary<string, object> report = Form.Check(form);

        Assert.False((bool)report["isValid"]);
        Assert.Contains(
            (List<string>)report["messages"],
            message => message.Contains("nosuchfield", StringComparison.Ordinal));
    }

    [Fact]
    public void Form_Check_reports_a_loop_between_computed_values()
    {
        FormDefinition form = Form.Create("Test", Elements(
            Behavior.WithComputed(Input.Number("A", key: "a"), Compute.Field("b")),
            Behavior.WithComputed(Input.Number("B", key: "b"), Compute.Field("a"))));

        Dictionary<string, object> report = Form.Check(form);

        Assert.False((bool)report["isValid"]);
        Assert.Contains(
            (List<string>)report["messages"],
            message => message.Contains("loop", StringComparison.Ordinal));
    }

    /// <summary>
    /// The trigger gate is what makes a form usable in a graph that re-executes: false means
    /// "do not ask again", and the answer must be the last one given, not a fresh set of defaults.
    /// </summary>
    [Fact]
    public void A_closed_trigger_returns_the_defaults_without_showing_anything()
    {
        Form.Forget();

        Dictionary<string, object> result = Form.Show(
            "Gated",
            Elements(Input.TextBox("Name", "Ada")),
            trigger: false,
            formId: "tests.gated");

        Assert.False((bool)result["wasSubmitted"]);
        Assert.Equal(FormButtonNames.Skipped, result["buttonClicked"]);

        Dictionary<string, object> values = (Dictionary<string, object>)result["values"];
        Assert.Equal("Ada", values["name"]);
    }

    [Fact]
    public void A_closed_trigger_returns_the_last_submitted_answers()
    {
        Form.Forget();

        FormDefinition form = Form.Create(
            "Gated",
            Elements(Input.TextBox("Name", "Ada")),
            formId: "tests.remembered");

        // Stand in for a run the user actually completed.
        FormSession session = new(form);
        session.SetValue("name", "Grace");
        SessionStore.Instance.Save("tests.remembered", session.BuildResult(true, FormButtonNames.Submit));

        Dictionary<string, object> result = Form.ShowDefinition(form, trigger: false);
        Dictionary<string, object> values = (Dictionary<string, object>)result["values"];

        Assert.Equal("Grace", values["name"]);

        Form.Forget("tests.remembered");
    }

    [Fact]
    public void Result_nodes_read_the_values_dictionary_and_the_form_object_alike()
    {
        Form.Forget();

        Dictionary<string, object> result = Form.Show(
            "Reading",
            Elements(
                Input.TextBox("Name", "Ada"),
                Input.Number("Height", 1.75),
                Input.CheckBox("Active", true),
                Input.ColorPicker("Tint", "#3366CC")),
            trigger: false,
            formId: "tests.reading");

        object values = result["values"];
        object form = result["form"];

        Assert.Equal("Ada", Result.GetString(values, "name"));
        Assert.Equal("Ada", Result.GetString(form, "name"));
        Assert.Equal(1.75d, Result.GetNumber(values, "height"));
        Assert.True(Result.GetBool(values, "active"));
        Assert.Equal("#3366CC", Result.GetColor(values, "tint")["hex"]);
    }

    [Fact]
    public void Result_accessors_fall_back_rather_than_returning_null()
    {
        Dictionary<string, object?> empty = new();

        Assert.Equal("none", Result.GetString(empty, "missing", "none"));
        Assert.Equal(42d, Result.GetNumber(empty, "missing", 42));
        Assert.True(Result.GetBool(empty, "missing", true));
        Assert.Empty(Result.GetFilePaths(empty, "missing"));
        Assert.Empty(Result.GetList(empty, "missing"));
    }

    [Fact]
    public void Result_GetList_wraps_a_single_answer_so_downstream_nodes_need_not_care()
    {
        Dictionary<string, object?> values = new() { ["one"] = "a", ["many"] = new List<object?> { "a", "b" } };

        Assert.Single(Result.GetList(values, "one"));
        Assert.Equal(2, Result.GetList(values, "many").Count);
    }

    [Fact]
    public void Theme_nodes_produce_the_palette_they_describe()
    {
        Theming.ThemeDefinition dark = Theme.Dark("#FF8800");

        Assert.Equal(Theming.AppearanceMode.Dark, dark.Mode);
        Assert.Equal(RgbColor.Parse("#FF8800"), dark.Accent);
    }

    [Fact]
    public void Form_options_reach_the_definition()
    {
        FormDefinition form = Form.Create(
            "Test",
            Elements(Input.TextBox("Name")),
            options: Form.Options(description: "Please fill this in.", resizable: false, showCancel: false));

        Assert.Equal("Please fill this in.", form.Description);
        Assert.False(form.Window.IsResizable);
        Assert.False(form.Buttons.ShowCancel);
    }

    [Fact]
    public void A_form_built_from_nodes_round_trips_through_JSON()
    {
        FormDefinition form = Form.Create("Round trip", Elements(
            Input.TextBox("Name"),
            Behavior.VisibleIf(Input.Number("Height"), Condition.IsNotEmpty("name")),
            Layout.Section("Details", new List<FormElement>
            {
                Input.DropDown("Mode", new List<object> { "a", "b" }),
            })));

        FormDefinition restored = Form.FromJson(Form.ToJson(form));

        Assert.Equal(Form.ToJson(form), Form.ToJson(restored));
    }
}
