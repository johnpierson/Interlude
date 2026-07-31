using System.Collections.Generic;
using System.IO;
using System.Linq;
using Interlude.Model;
using Interlude.Rendering.Wpf;
using Interlude.Runtime;
using Interlude.Serialization;
using Xunit;

namespace Interlude.Tests;

/// <summary>
/// Checks the example forms in <c>samples/</c>.
///
/// Those files are documentation — they are what a reader opens to see what a form looks like as
/// data, and what the preview harness loads. Documentation that no longer parses is worse than
/// none, so the schema and the examples are kept honest against each other here. Regenerate them
/// with <c>Interlude.Preview.exe --export samples</c>.
/// </summary>
public class SampleFormTests
{
    public static TheoryData<string> SampleFiles()
    {
        TheoryData<string> files = new();

        string folder = Path.Combine(RepoPaths.Root, "samples");
        if (!Directory.Exists(folder))
        {
            return files;
        }

        foreach (string file in Directory.EnumerateFiles(folder, "*.json").OrderBy(path => path))
        {
            files.Add(Path.GetFileName(file));
        }

        return files;
    }

    [Fact]
    public void The_samples_folder_is_not_empty()
    {
        Assert.NotEmpty(SampleFiles());
    }

    [Theory]
    [MemberData(nameof(SampleFiles))]
    public void A_sample_loads_and_round_trips(string fileName)
    {
        FormDefinition form = FormJson.Load(Path.Combine(RepoPaths.Root, "samples", fileName));

        Assert.NotEmpty(form.Title);
        Assert.NotEmpty(form.Elements);

        FormDefinition restored = FormJson.Deserialize(FormJson.Serialize(form));
        Assert.Equal(FormJson.Serialize(form), FormJson.Serialize(restored));
    }

    [Theory]
    [MemberData(nameof(SampleFiles))]
    public void A_sample_builds_a_session_with_nothing_to_warn_about(string fileName)
    {
        FormDefinition form = FormJson.Load(Path.Combine(RepoPaths.Root, "samples", fileName));

        FormSession session = new(form);

        Assert.Empty(session.Warnings);
        Assert.All(form.Inputs(), input => Assert.NotEmpty(input.Key));
    }

    [Theory]
    [MemberData(nameof(SampleFiles))]
    public void A_sample_uses_only_controls_this_build_can_draw(string fileName)
    {
        FormDefinition form = FormJson.Load(Path.Combine(RepoPaths.Root, "samples", fileName));
        ControlRendererRegistry registry = ControlRendererRegistry.CreateDefault();

        List<string> unrenderable = form.AllElements()
            .Where(element => !registry.CanRender(element))
            .Select(element => element.GetType().Name)
            .Distinct()
            .ToList();

        Assert.True(
            unrenderable.Count == 0,
            $"{fileName} uses controls with no renderer: " + string.Join(", ", unrenderable));
    }

    /// <summary>
    /// Every sample renders for real. This is the one test that would notice a theme resource a
    /// sample happens to need but no other test exercises.
    /// </summary>
    [WpfTheory]
    [MemberData(nameof(SampleFiles))]
    public void A_sample_renders(string fileName)
    {
        WpfTestContext.EnsureApplication();

        FormDefinition form = FormJson.Load(Path.Combine(RepoPaths.Root, "samples", fileName));
        FormSession session = new(form);
        FormWindow window = new(form, session, ControlRendererRegistry.CreateDefault());

        try
        {
            Assert.Equal(form.AllElements().Count(), window.Context.Views.Count);
        }
        finally
        {
            window.Close();
        }
    }
}
