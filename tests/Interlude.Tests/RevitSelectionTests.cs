using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using Interlude.Model;
using Interlude.Rendering.Wpf;
using Interlude.Rendering.Wpf.Controls;
using Interlude.Runtime;
using Xunit;

namespace Interlude.Tests;

/// <summary>
/// The Revit selection field, tested everywhere except Revit.
///
/// The reflection path itself can only run inside Revit, so what is pinned here is everything
/// around it: that the bridge reports itself honestly unavailable in any other process, that the
/// element coerces shapes the way the other multi-value inputs do, and that the control behaves —
/// via the picker override, which is the same seam the renderer calls through.
/// </summary>
public class RevitSelectionTests
{
    [Fact]
    public void The_bridge_is_unavailable_outside_Revit_and_says_why()
    {
        string? reason = RevitSelectionBridge.UnavailableReason();

        Assert.NotNull(reason);
        Assert.Contains("Revit", reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Picking_outside_Revit_fails_with_words_rather_than_throwing()
    {
        RevitSelectionOutcome outcome = RevitSelectionBridge.Pick(allowMultiple: true, prompt: null);

        Assert.NotNull(outcome.Failure);
        Assert.False(outcome.WasCancelled);
        Assert.Empty(outcome.Elements);
    }

    [Fact]
    public void A_picker_override_makes_the_bridge_available()
    {
        RevitSelectionBridge.OverridePicker = (_, _) => RevitSelectionOutcome.Picked(new object[] { "wall" });

        try
        {
            Assert.Null(RevitSelectionBridge.UnavailableReason());
            Assert.Equal(new object[] { "wall" }, RevitSelectionBridge.Pick(true, null).Elements);
        }
        finally
        {
            RevitSelectionBridge.OverridePicker = null;
        }
    }

    [Fact]
    public void Describe_prefers_a_Name_property_and_falls_back_to_ToString()
    {
        Assert.Equal("North Wall", RevitSelectionBridge.Describe(new { Name = "North Wall" }));
        Assert.Equal("just text", RevitSelectionBridge.Describe("just text"));
        Assert.Equal(string.Empty, RevitSelectionBridge.Describe(null));
    }

    [Fact]
    public void The_node_builds_the_element()
    {
        FormElement element = Input.SelectElements(
            "Rooms",
            allowMultiple: false,
            buttonText: "Pick…",
            prompt: "Pick a room.",
            key: "rooms");

        ModelSelectionElement selection = Assert.IsType<ModelSelectionElement>(element);
        Assert.False(selection.AllowMultiple);
        Assert.Equal("Pick…", selection.ButtonText);
        Assert.Equal("Pick a room.", selection.Prompt);
        Assert.Equal("rooms", selection.Key);
    }

    [Fact]
    public void Empty_button_text_and_prompt_mean_the_stock_ones()
    {
        ModelSelectionElement selection =
            (ModelSelectionElement)Input.SelectElements("Rooms", buttonText: "", prompt: " ");

        Assert.Null(selection.ButtonText);
        Assert.Null(selection.Prompt);
    }

    [Fact]
    public void A_multi_select_field_stores_a_list_whatever_arrives()
    {
        ModelSelectionElement element = new() { AllowMultiple = true };

        Assert.Equal(new object?[] { "a", "b" }, element.Coerce(new object?[] { "a", null, "b" }));
        Assert.Equal(new object?[] { "a" }, element.Coerce("a"));
        Assert.Empty((IEnumerable<object?>)element.Coerce(null)!);
        Assert.Empty((IEnumerable<object?>)element.GetFallbackValue()!);
    }

    [Fact]
    public void A_single_select_field_stores_one_element_whatever_arrives()
    {
        ModelSelectionElement element = new() { AllowMultiple = false };

        Assert.Equal("a", element.Coerce(new object?[] { "a", "b" }));
        Assert.Equal("a", element.Coerce("a"));
        Assert.Null(element.Coerce(null));
        Assert.Null(element.GetFallbackValue());
    }

    [Fact]
    public void A_cancelled_form_returns_the_default_selection_not_null()
    {
        FormDefinition form = Form.Create("Test", new List<object>
        {
            Input.SelectElements("Rooms", key: "rooms"),
        });

        FormSession session = new(form);
        FormResult cancelled = session.BuildCancelledResult(FormButtonNames.Cancel);

        Assert.False(cancelled.WasSubmitted);
        Assert.Empty((IEnumerable<object?>)cancelled.Values["rooms"]!);
    }

    [WpfFact]
    public void Outside_Revit_the_button_is_disabled_and_the_summary_says_why()
    {
        WpfTestContext.EnsureApplication();

        ModelSelectionBox box = new(new ModelSelectionElement { Key = "rooms" });

        Button button = box.Children.OfType<Button>().Single();
        TextBlock summary = box.Children.OfType<TextBlock>().Single();

        Assert.False(button.IsEnabled);
        Assert.Contains("Revit", summary.Text, StringComparison.Ordinal);
    }

    [WpfFact]
    public void Clicking_the_button_picks_through_the_bridge_and_reports_the_value()
    {
        WpfTestContext.EnsureApplication();

        RevitSelectionBridge.OverridePicker = (allowMultiple, _) =>
        {
            Assert.True(allowMultiple);
            return RevitSelectionOutcome.Picked(new object[] { "wall", "door" });
        };

        try
        {
            ModelSelectionBox box = new(new ModelSelectionElement { Key = "rooms", AllowMultiple = true });
            bool raised = false;
            box.ValueChanged += (_, _) => raised = true;

            Button button = box.Children.OfType<Button>().Single();
            Assert.True(button.IsEnabled);
            button.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));

            Assert.True(raised);
            Assert.Equal(new object?[] { "wall", "door" }, (IEnumerable<object?>)box.Value!);
        }
        finally
        {
            RevitSelectionBridge.OverridePicker = null;
        }
    }

    [WpfFact]
    public void Cancelling_a_pick_keeps_the_previous_answer()
    {
        WpfTestContext.EnsureApplication();

        RevitSelectionBridge.OverridePicker = (_, _) => RevitSelectionOutcome.Cancelled();

        try
        {
            ModelSelectionBox box = new(new ModelSelectionElement { Key = "rooms" })
            {
                Value = new object?[] { "wall" },
            };

            bool raised = false;
            box.ValueChanged += (_, _) => raised = true;

            Button button = box.Children.OfType<Button>().Single();
            button.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));

            Assert.False(raised);
            Assert.Equal(new object?[] { "wall" }, (IEnumerable<object?>)box.Value!);
        }
        finally
        {
            RevitSelectionBridge.OverridePicker = null;
        }
    }

    [WpfFact]
    public void The_renderer_reads_and_writes_through_the_box()
    {
        WpfTestContext.EnsureApplication();

        ModelSelectionRenderer renderer = new();
        ModelSelectionBox box = new(new ModelSelectionElement { Key = "rooms" });

        renderer.WriteValue(box, new object?[] { "wall" });

        Assert.Equal(new object?[] { "wall" }, (IEnumerable<object?>)renderer.ReadValue(box)!);
    }
}
