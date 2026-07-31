using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Interlude.Model;
using Interlude.Runtime;
using Xunit;

namespace Interlude.Tests;

/// <summary>
/// The pieces that make a form survive Dynamo's execution model: the re-entrancy latch, the
/// remembered-answers store, and host detection.
/// </summary>
public class RuntimeTests
{
    [Fact]
    public void The_store_remembers_a_submitted_result()
    {
        SessionStore store = new();
        FormDefinition form = TestForms.Form(TestForms.Text("name", "default"));
        FormSession session = new(form);
        session.SetValue("name", "Ada");

        store.Save("form.a", session.BuildResult(true, FormButtonNames.Submit));

        Assert.True(store.TryGet("form.a", out FormResult? remembered));
        Assert.Equal("Ada", remembered!.Values["name"]);
    }

    /// <summary>
    /// Cancelling must not destroy the answers from the last completed run. Backing out of a
    /// dialog to check something and losing twenty fields is the behaviour this rule exists to
    /// prevent.
    /// </summary>
    [Fact]
    public void The_store_ignores_a_cancelled_result()
    {
        SessionStore store = new();
        FormDefinition form = TestForms.Form(TestForms.Text("name", "default"));
        FormSession session = new(form);
        session.SetValue("name", "Ada");

        store.Save("form.b", session.BuildResult(true, FormButtonNames.Submit));
        store.Save("form.b", session.BuildCancelledResult());

        Assert.True(store.TryGet("form.b", out FormResult? remembered));
        Assert.Equal("Ada", remembered!.Values["name"]);
    }

    [Fact]
    public void Clearing_the_store_forgets_everything()
    {
        SessionStore store = new();
        FormDefinition form = TestForms.Form(TestForms.Text("name"));
        store.Save("form.c", new FormSession(form).BuildResult(true, FormButtonNames.Submit));

        Assert.Equal(1, store.Count);

        store.Clear();
        Assert.Equal(0, store.Count);
    }

    /// <summary>
    /// The behaviour that turns an Automatic-mode dialog storm into one dialog: a second caller
    /// waits for the first window rather than opening its own.
    /// </summary>
    [Fact]
    public async Task A_second_call_waits_for_the_form_already_showing()
    {
        FormLatch latch = new();
        FormDefinition form = TestForms.Form(TestForms.Text("name", "default"));

        using ManualResetEventSlim firstIsRunning = new(false);
        using ManualResetEventSlim secondIsEntering = new(false);
        using ManualResetEventSlim releaseFirst = new(false);

        FormResult expected = new FormSession(form).BuildResult(true, "first");
        int windowsOpened = 0;

        Task<FormResult> first = Task.Run(() => latch.Run(
            "shared",
            () =>
            {
                Interlocked.Increment(ref windowsOpened);
                firstIsRunning.Set();
                releaseFirst.Wait(TimeSpan.FromSeconds(10));
                return expected;
            },
            () => FormResult.Cancelled(form)));

        Assert.True(firstIsRunning.Wait(TimeSpan.FromSeconds(10)));
        Assert.True(latch.IsShowing("shared"));

        Task<FormResult> second = Task.Run(() =>
        {
            secondIsEntering.Set();

            return latch.Run(
                "shared",
                () =>
                {
                    Interlocked.Increment(ref windowsOpened);
                    return new FormSession(form).BuildResult(true, "second");
                },
                () => FormResult.Cancelled(form));
        });

        // The first call is held open until the second has entered and settled into its wait.
        // Without that the second could arrive after the first had already finished, which is a
        // legitimate second showing and would test nothing.
        Assert.True(secondIsEntering.Wait(TimeSpan.FromSeconds(10)));
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        releaseFirst.Set();

        FormResult[] results = await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(20));

        Assert.Equal(1, Volatile.Read(ref windowsOpened));
        Assert.Same(expected, results[0]);
        Assert.Same(expected, results[1]);
    }

    [Fact]
    public void Different_forms_do_not_block_each_other()
    {
        FormLatch latch = new();
        FormDefinition form = TestForms.Form(TestForms.Text("name"));

        FormResult a = latch.Run("a", () => new FormSession(form).BuildResult(true, "a"), () => FormResult.Cancelled(form));
        FormResult b = latch.Run("b", () => new FormSession(form).BuildResult(true, "b"), () => FormResult.Cancelled(form));

        Assert.Equal("a", a.ButtonClicked);
        Assert.Equal("b", b.ButtonClicked);
    }

    [Fact]
    public void The_latch_releases_when_showing_a_form_throws()
    {
        FormLatch latch = new();
        FormDefinition form = TestForms.Form(TestForms.Text("name"));

        Assert.Throws<InvalidOperationException>(() => latch.Run(
            "boom",
            () => throw new InvalidOperationException("boom"),
            () => FormResult.Cancelled(form)));

        Assert.False(latch.IsShowing("boom"));
    }

    [Theory]
    [InlineData("DynamoCLI", true)]
    [InlineData("DynamoWPFCLI", true)]
    [InlineData("GenerativeDesign.Executive", true)]
    [InlineData("Revit", false)]
    [InlineData("DynamoSandbox", false)]
    public void Known_command_line_hosts_are_recognised(string processName, bool expected)
    {
        HostContext host = HostContext.Create(processName, isUserInteractive: true);

        Assert.Equal(expected, host.IsKnownHeadlessProcess);
    }

    [Fact]
    public void A_process_with_no_desktop_session_looks_headless_whatever_it_is_called()
    {
        HostContext host = HostContext.Create("Revit", isUserInteractive: false);

        Assert.True(host.LooksHeadless);
    }

    [Fact]
    public void The_headless_error_explains_how_to_proceed()
    {
        HeadlessFormException error = new("My Form", HostContext.Create("DynamoCLI", true));

        Assert.Contains("My Form", error.Message, StringComparison.Ordinal);
        Assert.Contains("DynamoCLI", error.Message, StringComparison.Ordinal);
        Assert.Contains("headlessUseDefaults", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Form identity has to be stable across runs or remembered answers never come back, and it
    /// has to change when the form does or answers leak between different forms.
    /// </summary>
    [Fact]
    public void A_derived_form_id_is_stable_for_the_same_form_and_different_for_another()
    {
        FormDefinition first = TestForms.Form(TestForms.Text("name"), TestForms.Number("count"));
        FormDefinition same = TestForms.Form(TestForms.Text("name"), TestForms.Number("count"));
        FormDefinition different = TestForms.Form(TestForms.Text("name"));

        Assert.Equal(first.ResolveFormId(), same.ResolveFormId());
        Assert.NotEqual(first.ResolveFormId(), different.ResolveFormId());
    }

    [Fact]
    public void An_explicit_form_id_is_used_as_given()
    {
        FormDefinition form = TestForms.Form(TestForms.Text("name")) with { FormId = "  my.form  " };

        Assert.Equal("my.form", form.ResolveFormId());
    }

    /// <summary>
    /// The culture rule in one test: a value typed on a German machine and a value from JSON must
    /// mean the same number, and comparisons must not change answer with the machine's locale.
    /// </summary>
    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    public void Values_compare_the_same_whatever_the_machine_locale(string culture)
    {
        CultureInfo original = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);

            Assert.True(Conditions.ValueOps.AreEqual("1.5", 1.5d));
            Assert.True(Conditions.ValueOps.TryCompare(10d, "9", out int comparison) && comparison > 0);
            Assert.Equal("1.5", Conditions.ValueOps.ToStringInvariant(1.5d));
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = original;
        }
    }

    [Fact]
    public void Colours_round_trip_through_hex_in_every_supported_form()
    {
        Assert.Equal(new RgbColor(0x33, 0x66, 0xCC), RgbColor.Parse("#3366CC"));
        Assert.Equal(new RgbColor(0x00, 0xAA, 0xFF), RgbColor.Parse("#0af"));
        Assert.Equal(new RgbColor(0x33, 0x66, 0xCC, 0x80), RgbColor.Parse("#803366CC"));
        Assert.Equal("#3366CC", RgbColor.Parse("3366CC").ToHex());
        Assert.False(RgbColor.TryParse("not a colour", out _));
    }
}
