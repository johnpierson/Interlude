using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Interlude.Conditions;
using Interlude.Model;
using Interlude.Rendering.Wpf.Controls;
using Interlude.Runtime;
using Interlude.Serialization;
using Interlude.Theming;
using Interlude.Validation;
using Xunit;
using ValidationRule = Interlude.Validation.ValidationRule;

namespace Interlude.Tests;

/// <summary>Regression coverage for the audit's state, serialization and control failure modes.</summary>
public class SlopRegressionTests
{
    [Fact]
    public void State_and_option_matching_preserve_identity_for_same_text_objects()
    {
        SameTextObject first = new(1);
        SameTextObject second = new(2);
        OptionItem[] options = { OptionItem.FromValue(first), OptionItem.FromValue(second) };

        Assert.Same(second, OptionItem.Find(options, second)!.Value);

        ListSelectionElement list = new() { AllowMultiple = false, Options = options };
        Assert.Same(second, list.Coerce(second));

        TreeSelectionElement tree = new()
        {
            AllowMultiple = false,
            Roots = new[]
            {
                new TreeNode { Display = "First", Value = first },
                new TreeNode { Display = "Second", Value = second },
            },
        };
        Assert.Same(second, tree.Coerce(second));

        FormStateStore store = new();
        Assert.True(store.Set("choice", first));
        Assert.True(store.Set("choice", second));
        Assert.Same(second, store.GetValue("choice"));
    }

    [Fact]
    public void Range_rejects_non_finite_numbers()
    {
        RangeRule rule = new() { Minimum = 0d, Maximum = 10d };
        FormStateStore state = new();

        Assert.False(rule.Validate(double.NaN, state).IsValid);
        Assert.False(rule.Validate(double.PositiveInfinity, state).IsValid);
        Assert.False(rule.Validate(double.NegativeInfinity, state).IsValid);
    }

    [Fact]
    public void Invalid_node_arguments_are_rejected_instead_of_defaulting()
    {
        Assert.Throws<ArgumentException>(() => Compute.Arithmetic(1, "Mulitply", 2));
        Assert.Throws<InterludeException>(() => Rule.Range("ten"));
    }

    [Fact]
    public void Nested_value_objects_round_trip_as_objects()
    {
        Dictionary<string, object?> values = FormJson.DeserializeValues("{\"answer\":{\"x\":1}}");

        Assert.Equal("{\"answer\":{\"x\":1}}", FormJson.SerializeValues(values, indented: false));
    }

    [Fact]
    public void Custom_validation_rules_are_rejected_at_the_JSON_boundary()
    {
        FormDefinition form = new()
        {
            Elements = new FormElement[]
            {
                new TextBoxElement
                {
                    Key = "name",
                    Rules = new ValidationRule[]
                    {
                        new CustomPredicateRule { Predicate = (_, _) => false },
                    },
                },
            },
        };

        Assert.Throws<InterludeJsonException>(() => FormJson.Serialize(form));
    }

    [Fact]
    public void Bulk_mutations_report_direct_value_changes()
    {
        FormSession session = new(TestForms.Form(TestForms.Text("name", "initial")));
        List<ElementStateChange> changes = new();
        int batches = 0;
        session.Changed += (_, args) =>
        {
            batches++;
            changes.AddRange(args.Changes);
        };

        session.SetValues(new Dictionary<string, object?> { ["name"] = "updated" });

        Assert.Equal(1, batches);
        Assert.Contains(changes, change => change.State.Key == "name" && change.Includes(StateChangeKind.Value));

        changes.Clear();
        session.Reset();

        Assert.Equal(2, batches);
        Assert.Contains(changes, change => change.State.Key == "name" && change.Includes(StateChangeKind.Value));
    }

    [WpfFact]
    public void Clock_time_0930_does_not_shift_the_selected_date()
    {
        WpfTestContext.EnsureApplication();
        DateTimeField field = new(new DatePickerElement { IncludeTime = true });
        field.Write(new DateTime(2026, 9, 4));
        field.Children.OfType<TextBox>().Single().Text = "0930";

        Assert.Equal(new DateTime(2026, 9, 4, 9, 30, 0), field.Read());
    }

    [WpfFact]
    public void Read_only_numeric_fields_ignore_spinner_steps()
    {
        WpfTestContext.EnsureApplication();
        NumericBox box = new(null, null, 1d, 2, null, showSpinner: true, isInteger: false);
        box.Entry.IsReadOnly = true;
        box.Write(5d);

        StackPanel spinner = ((Grid)box.Child).Children.OfType<StackPanel>().Single();
        spinner.Children.OfType<RepeatButton>().First().RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

        Assert.Equal(5d, box.Value);
    }

    [WpfFact]
    public void Single_select_trees_clear_stale_selection_on_empty_writes()
    {
        WpfTestContext.EnsureApplication();
        TreeSelectionElement element = new()
        {
            AllowMultiple = false,
            Roots = new[] { new TreeNode { Display = "A", Value = "a" } },
        };
        SelectionTree tree = new(element, null!);

        tree.Write("a");
        tree.Write(null);

        Assert.Null(tree.Read());
    }

    [WpfFact]
    public void Dialog_selected_paths_keep_semicolons_inside_a_filename()
    {
        WpfTestContext.EnsureApplication();
        PathBox box = new(new FilePickerElement { AllowMultiple = true }, null, isFolder: false);
        string path = @"C:\work\phase;one.txt";

        box.SetPaths(new[] { path });

        Assert.Equal(new[] { path }, box.Paths);
    }

    private sealed class SameTextObject
    {
        internal SameTextObject(int id) => Id = id;

        internal int Id { get; }

        public override string ToString() => "same";
    }
}
