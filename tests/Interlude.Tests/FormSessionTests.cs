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
/// The bulk of Interlude's behaviour lives in <see cref="FormSession"/> and none of it needs a
/// window. Feed values in, assert visibility, enablement, computed values and errors out.
/// </summary>
public class FormSessionTests
{
    [Fact]
    public void A_new_session_starts_at_each_field_default()
    {
        FormSession session = new(TestForms.Form(
            TestForms.Text("name", "Ada"),
            TestForms.Number("count", 3d)));

        Assert.Equal("Ada", session.GetValue("name"));
        Assert.Equal(3d, session.GetValue("count"));
    }

    [Fact]
    public void Setting_a_value_shows_and_hides_the_fields_that_depend_on_it()
    {
        TextBoxElement reason = TestForms.Text("reason") with
        {
            VisibleIf = new ComparisonCondition { Key = "mode", Operator = ComparisonOperator.Equals, Operand = "custom" },
        };

        FormSession session = new(TestForms.Form(TestForms.Dropdown("mode", "auto", "custom"), reason));

        Assert.False(session.GetState("reason")!.IsVisible);

        session.SetValue("mode", "custom");
        Assert.True(session.GetState("reason")!.IsVisible);

        session.SetValue("mode", "auto");
        Assert.False(session.GetState("reason")!.IsVisible);
    }

    [Fact]
    public void Hiding_a_container_hides_everything_inside_it()
    {
        FormDefinition form = TestForms.Form(
            TestForms.Check("advanced"),
            new GroupBoxElement
            {
                Header = "Advanced",
                VisibleIf = new ComparisonCondition { Key = "advanced", Operator = ComparisonOperator.IsChecked },
                Children = new FormElement[] { TestForms.Text("secret") },
            });

        FormSession session = new(form);
        Assert.False(session.GetState("secret")!.IsVisible);

        session.SetValue("advanced", true);
        Assert.True(session.GetState("secret")!.IsVisible);
    }

    [Fact]
    public void Disabling_a_container_disables_everything_inside_it()
    {
        FormDefinition form = TestForms.Form(
            TestForms.Check("unlocked"),
            new GroupBoxElement
            {
                Header = "Settings",
                EnabledIf = new ComparisonCondition { Key = "unlocked", Operator = ComparisonOperator.IsChecked },
                Children = new FormElement[] { TestForms.Text("setting") },
            });

        FormSession session = new(form);
        Assert.False(session.GetState("setting")!.IsEnabled);
        Assert.True(session.GetState("setting")!.IsVisible);

        session.SetValue("unlocked", true);
        Assert.True(session.GetState("setting")!.IsEnabled);
    }

    [Fact]
    public void Computed_values_update_when_their_inputs_change()
    {
        InputElement total = TestForms.Number("total") with
        {
            Computed = new ArithmeticComputed
            {
                Operator = ArithmeticOperator.Multiply,
                Left = new FieldComputed { Key = "quantity" },
                Right = new FieldComputed { Key = "price" },
            },
        };

        FormSession session = new(TestForms.Form(
            TestForms.Number("quantity", 2d),
            TestForms.Number("price", 10d),
            total));

        Assert.Equal(20d, session.GetValue("total"));

        session.SetValue("quantity", 5d);
        Assert.Equal(50d, session.GetValue("total"));
    }

    /// <summary>
    /// The whole point of the dependency graph: a chain must settle in one pass, with each
    /// link seeing its input already updated rather than the previous run's value.
    /// </summary>
    [Fact]
    public void A_chain_of_computed_values_settles_in_a_single_pass()
    {
        InputElement doubled = TestForms.Number("doubled") with
        {
            Computed = new ArithmeticComputed
            {
                Operator = ArithmeticOperator.Multiply,
                Left = new FieldComputed { Key = "seed" },
                Right = new ConstantComputed { Value = 2d },
            },
        };

        InputElement quadrupled = TestForms.Number("quadrupled") with
        {
            Computed = new ArithmeticComputed
            {
                Operator = ArithmeticOperator.Multiply,
                Left = new FieldComputed { Key = "doubled" },
                Right = new ConstantComputed { Value = 2d },
            },
        };

        // Declared out of dependency order on purpose: the graph, not the author, decides
        // evaluation order.
        FormSession session = new(TestForms.Form(quadrupled, doubled, TestForms.Number("seed", 1d)));

        Assert.Equal(4d, session.GetValue("quadrupled"));

        session.SetValue("seed", 3d);
        Assert.Equal(6d, session.GetValue("doubled"));
        Assert.Equal(12d, session.GetValue("quadrupled"));
    }

    [Fact]
    public void A_computed_field_ignores_writes()
    {
        InputElement computed = TestForms.Number("total") with
        {
            Computed = new FieldComputed { Key = "source" },
        };

        FormSession session = new(TestForms.Form(TestForms.Number("source", 7d), computed));

        Assert.False(session.SetValue("total", 999d));
        Assert.Equal(7d, session.GetValue("total"));
    }

    [Fact]
    public void Computed_values_that_depend_on_each_other_are_rejected_before_a_window_opens()
    {
        InputElement a = TestForms.Number("a") with { Computed = new FieldComputed { Key = "b" } };
        InputElement b = TestForms.Number("b") with { Computed = new FieldComputed { Key = "a" } };

        FormCycleException error = Assert.Throws<FormCycleException>(
            () => new FormSession(TestForms.Form(a, b)));

        Assert.Contains("a", error.Cycle);
        Assert.Contains("b", error.Cycle);
    }

    [Fact]
    public void A_field_computed_from_its_own_value_is_rejected_as_a_cycle()
    {
        InputElement self = TestForms.Number("total") with
        {
            Computed = new ArithmeticComputed
            {
                Operator = ArithmeticOperator.Add,
                Left = new FieldComputed { Key = "total" },
                Right = new ConstantComputed { Value = 1d },
            },
        };

        FormCycleException error = Assert.Throws<FormCycleException>(
            () => new FormSession(TestForms.Form(self)));

        Assert.Equal(new[] { "total" }, error.Cycle);
    }

    [Fact]
    public void Required_fields_block_submission_until_answered()
    {
        FormElement name = TestForms.Text("name") with { RequiredIf = ConstantCondition.True };
        FormSession session = new(TestForms.Form(name));

        Assert.False(session.TrySubmit(FormButtonNames.Submit, out FormResult? failed));
        Assert.Null(failed);
        Assert.Equal("This field is required.", session.Errors["name"]);

        session.SetValue("name", "Ada");

        Assert.True(session.TrySubmit(FormButtonNames.Submit, out FormResult? passed));
        Assert.Equal("Ada", passed!.Values["name"]);
    }

    /// <summary>
    /// A required field the user cannot see must never block them. This is the failure mode
    /// that makes conditional forms unusable: an invisible error with no control to fix it.
    /// </summary>
    [Fact]
    public void A_hidden_required_field_does_not_block_submission()
    {
        FormElement reason = TestForms.Text("reason") with
        {
            RequiredIf = ConstantCondition.True,
            VisibleIf = new ComparisonCondition { Key = "needsReason", Operator = ComparisonOperator.IsChecked },
        };

        FormSession session = new(TestForms.Form(TestForms.Check("needsReason"), reason));

        Assert.True(session.TrySubmit(FormButtonNames.Submit, out _));

        session.SetValue("needsReason", true);
        Assert.False(session.TrySubmit(FormButtonNames.Submit, out _));
    }

    [Fact]
    public void RequiredIf_tracks_the_field_it_depends_on()
    {
        FormElement licence = TestForms.Text("licence") with
        {
            RequiredIf = new ComparisonCondition { Key = "isPro", Operator = ComparisonOperator.IsChecked },
        };

        FormSession session = new(TestForms.Form(TestForms.Check("isPro"), licence));

        Assert.False(session.GetState("licence")!.IsRequired);

        session.SetValue("isPro", true);
        Assert.True(session.GetState("licence")!.IsRequired);
    }

    [Fact]
    public void Validation_rules_run_as_the_value_changes()
    {
        FormElement age = TestForms.Number("age") with
        {
            Rules = new ValidationRule[] { new RangeRule { Minimum = 18d, Maximum = 120d } },
        };

        FormSession session = new(TestForms.Form(age));

        session.SetValue("age", 10d);
        Assert.False(session.GetState("age")!.IsValid);

        session.SetValue("age", 30d);
        Assert.True(session.GetState("age")!.IsValid);
    }

    [Fact]
    public void A_rule_that_reads_another_field_re_runs_when_that_field_changes()
    {
        FormElement end = TestForms.Number("end") with
        {
            Rules = new ValidationRule[]
            {
                new ComparisonRule { OtherKey = "start", Operator = ComparisonOperator.GreaterThan },
            },
        };

        FormSession session = new(TestForms.Form(TestForms.Number("start", 5d), end));

        session.SetValue("end", 3d);
        Assert.False(session.GetState("end")!.IsValid);

        session.SetValue("start", 1d);
        Assert.True(session.GetState("end")!.IsValid);
    }

    [Fact]
    public void One_edit_raises_exactly_one_event_however_far_it_cascades()
    {
        InputElement total = TestForms.Number("total") with
        {
            Computed = new SumComputed { Keys = new[] { "a", "b" } },
        };

        FormElement warning = TestForms.Text("warning") with
        {
            VisibleIf = new ComparisonCondition
            {
                Key = "total",
                Operator = ComparisonOperator.GreaterThan,
                Operand = 10d,
            },
        };

        FormSession session = new(TestForms.Form(
            TestForms.Number("a"), TestForms.Number("b"), total, warning));

        int events = 0;
        List<ElementStateChange> received = new();
        session.Changed += (_, args) =>
        {
            events++;
            received.AddRange(args.Changes);
        };

        session.SetValue("a", 20d);

        Assert.Equal(1, events);
        Assert.Contains(received, change => change.State.Key == "total" && change.Includes(StateChangeKind.Value));
        Assert.Contains(received, change => change.State.Key == "warning" && change.Includes(StateChangeKind.Visibility));
    }

    [Fact]
    public void Setting_a_value_to_what_it_already_is_raises_nothing()
    {
        FormSession session = new(TestForms.Form(TestForms.Text("name", "Ada")));

        int events = 0;
        session.Changed += (_, _) => events++;

        Assert.False(session.SetValue("name", "Ada"));
        Assert.Equal(0, events);
    }

    [Fact]
    public void Cancelling_returns_every_default_rather_than_nulls()
    {
        FormSession session = new(TestForms.Form(
            TestForms.Text("name", "Ada"),
            TestForms.Number("count", 7d),
            TestForms.Check("flag", true)));

        session.SetValue("name", "typed but abandoned");

        FormResult result = session.BuildCancelledResult();

        Assert.False(result.WasSubmitted);
        Assert.True(result.WasCancelled);
        Assert.Equal("Ada", result.Values["name"]);
        Assert.Equal(7d, result.Values["count"]);
        Assert.Equal(true, result.Values["flag"]);
        Assert.All(result.Values.Values, Assert.NotNull);
    }

    [Fact]
    public void A_cancelled_result_still_contains_every_key()
    {
        FormDefinition form = TestForms.Form(
            TestForms.Text("a"), TestForms.Number("b"), TestForms.Check("c"));

        FormResult result = FormResult.Cancelled(form);

        Assert.Equal(new[] { "a", "b", "c" }, result.Values.Keys.OrderBy(key => key));
        Assert.Equal(FormButtonNames.Cancel, result.ButtonClicked);
    }

    [Fact]
    public void Resetting_returns_every_field_to_its_default()
    {
        FormSession session = new(TestForms.Form(TestForms.Text("name", "Ada")));

        session.SetValue("name", "changed");
        session.Reset();

        Assert.Equal("Ada", session.GetValue("name"));
        Assert.False(session.GetState("name")!.IsTouched);
    }

    [Fact]
    public void Remembered_values_pre_fill_the_form()
    {
        FormDefinition form = TestForms.Form(TestForms.Text("name", "default"), TestForms.Number("count", 1d));

        FormSession session = new(form, new Dictionary<string, object?>
        {
            ["name"] = "remembered",
        });

        Assert.Equal("remembered", session.GetValue("name"));
        Assert.Equal(1d, session.GetValue("count"));
    }

    [Fact]
    public void A_remembered_value_for_a_field_that_no_longer_exists_is_ignored()
    {
        FormDefinition form = TestForms.Form(TestForms.Text("name"));

        FormSession session = new(form, new Dictionary<string, object?>
        {
            ["removedField"] = "stale",
        });

        Assert.False(session.Values.ContainsKey("removedField"));
    }

    [Fact]
    public void A_condition_on_a_key_no_field_uses_is_reported_as_a_warning()
    {
        FormElement field = TestForms.Text("visible") with
        {
            VisibleIf = new ComparisonCondition { Key = "typoed", Operator = ComparisonOperator.IsChecked },
        };

        FormSession session = new(TestForms.Form(field));

        Assert.Contains(session.Warnings, warning => warning.Contains("typoed", StringComparison.Ordinal));
    }

    [Fact]
    public void Errors_are_only_revealed_for_touched_fields_until_a_submit_is_attempted()
    {
        FormElement name = TestForms.Text("name") with { RequiredIf = ConstantCondition.True };
        FormSession session = new(TestForms.Form(name));

        Assert.False(session.ShowAllErrors);
        Assert.False(session.GetState("name")!.IsTouched);

        session.TrySubmit(FormButtonNames.Submit, out _);

        Assert.True(session.ShowAllErrors);
    }

    [Fact]
    public void FirstInvalid_finds_the_field_to_focus_in_document_order()
    {
        FormElement first = TestForms.Text("first") with { RequiredIf = ConstantCondition.True };
        FormElement second = TestForms.Text("second") with { RequiredIf = ConstantCondition.True };

        FormSession session = new(TestForms.Form(first, second));
        session.Validate();

        Assert.Equal("first", session.FirstInvalid()!.Key);
    }

    [Fact]
    public void Only_inputs_contribute_values_to_the_result()
    {
        FormSession session = new(TestForms.Form(
            new LabelElement { Text = "Heading" },
            new SeparatorElement(),
            TestForms.Text("name")));

        Assert.Equal(new[] { "name" }, session.Values.Keys);
    }
}
