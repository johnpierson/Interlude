# Contributing

Thanks for looking. Issues, questions and pull requests are all welcome.

## Getting set up

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download). One SDK compiles the whole
matrix — the `net8.0-windows` builds for Dynamo 3.0 and 3.6 as well as `net10.0-windows` for 4.0.

```bash
git clone https://github.com/johnpierson/Interlude.git
cd Interlude
dotnet build Interlude.sln
dotnet test Interlude.sln
```

```powershell
./scripts/build-all.ps1 -Pack   # all three Dynamo versions, packaged under dist/
```

The fastest way to see a change is the **preview harness**, which shows the sample gallery in
either theme and reloads a form from JSON when the file is saved:

```bash
dotnet run --project tools/Interlude.Preview
```

Read [docs/architecture.md](docs/architecture.md) before anything structural.

## The rules that are not negotiable

These are the ones with teeth. Each has a test that fails if it is broken, and each exists
because breaking it hurts someone else's Revit install rather than ours.

### One code assembly, no runtime dependencies

Interlude ships exactly one assembly containing code: `Interlude.dll`. A Dynamo package is
unzipped into a flat folder that Revit shares with every other add-in, so every extra DLL is a
version conflict waiting for the user who installs one more package.

Use the BCL, in-box WPF and in-box `System.Text.Json`. If you find yourself wanting a package,
that is the moment to discuss it in an issue rather than in a pull request.

`Interlude.customization.dll` sits beside it and is the one exception. Dynamo reads node icons
from a sibling assembly with that name and from nowhere else, so the file has to exist; it is
tolerable because it holds 224 PNGs, declares no types and references nothing. That is checked in
three places, and if you ever find yourself wanting to put a class in it, the answer is
`Interlude.dll`. See [Architecture](docs/architecture.md#the-one-exception-node-icons).

### Every node has an icon

Adding a node means adding it to the catalogue in
[`tools/Interlude.Preview/Icons.cs`](tools/Interlude.Preview/Icons.cs) and regenerating:

```
Interlude.Preview.exe --icons src/Interlude.Icons
```

Pick an existing glyph if one fits — reuse across categories is the design, not a shortcut, and
the plate colour already says which family the node is in. Both the generator and `NodeIconTests`
fail until every node is covered and no icon is left over, because a missing icon shows up as
Dynamo's default cube rather than as an error.

### The node API is append-only

Saved graphs bind to node names and to parameter *positions*. Renaming a method, reordering a
parameter, retyping a port or removing a `[MultiReturn]` name breaks every saved graph that used
it — and it breaks in someone else's project, months later, as a node that will not load.

So:

- New optional parameters go **on the end**. Always.
- New `[MultiReturn]` names go **on the end**.
- Nothing is deleted. Retire a node with `[IsVisibleInDynamoLibrary(false)]`.
- `AssemblyVersion` stays `1.0.0.0` for ever; only `FileVersion` and the package version move.

`ApiSurfaceTests` enforces this against
[`tests/Interlude.Tests/api-surface.txt`](tests/Interlude.Tests/api-surface.txt). When a change is
deliberate:

```bash
INTERLUDE_UPDATE_API=1 dotnet test tests/Interlude.Tests
```

Then read the diff before committing it. If the diff shows a line under `REMOVED OR CHANGED`,
stop and think again.

### Every public member must be importable by Dynamo

Dynamo imports a zero-touch assembly by reflecting over **every public type in it** and building
a DesignScript AST for every public constructor and method. `[IsVisibleInDynamoLibrary(false)]`
controls what appears in the *library*; it does not stop a type being *imported*.

So one member Dynamo's importer cannot parse does not hide one node — it throws
`LibraryLoadFailedException` and **not a single Interlude node loads**. The package appears empty,
with the reason buried in Dynamo's notification panel.

The rule in practice: **an optional parameter's default must be `null`, `bool`, `char`, `string`,
`int`, `long`, `double` or `float`.** Nothing else. `byte alpha = 255` is what took the whole
package down once, because `AstFactory.BuildPrimitiveNodeFromObject` has no case for `byte`.
Where you need a defaulted value of another type, write a second overload instead.

`ZeroTouchImportTests` enforces this two ways: it checks every public member against the rule,
and it runs Dynamo's real importer over the built assembly. The second is why the test project
references `DynamoVisualProgramming.Core` — the one deliberate exception to keeping Dynamo out of
the tests.

### Never write to the host's resources

Interlude is a guest in Revit's and Dynamo's process. Theming goes into the **form window's own**
`Resources` — never `Application.Current.Resources`.

### The layers below the renderer stay free of WPF

`Model`, `Conditions`, `Validation`, `Runtime`, `Serialization` and `Theming` must not reference
`System.Windows`. That is what keeps the interesting tests running without a UI thread, and what
keeps a second renderer possible.

### Slug rules are frozen

`FormKeys.Slugify` decides what a graph's `values["wall_type"]` means. Changing it is a breaking
change to every graph relying on a derived key, and means bumping `SlugVersion`.

## Adding a control

Four steps, each with a test that fails if you skip it:

1. A sealed `record` in `Model/InputElements.cs` (or `DisplayElements.cs` / `ContainerElements.cs`).
2. `[JsonDerivedType]` on `FormElement`, with a short stable discriminator.
3. An `IControlRenderer` in `Rendering/Wpf/Controls/`, registered in
   `ControlRendererRegistry.CreateDefault`.
4. A node in `Nodes/Input.cs` (or `Layout.cs`) with full XML docs — Dynamo shows them as the node
   and port tooltips.

Then add it to the preview gallery and regenerate the samples:

```powershell
dotnet build tools/Interlude.Preview
$exe = './tools/Interlude.Preview/bin/Debug/net10.0-windows/Interlude.Preview.exe'
& $exe --export samples            # regenerate samples/ (CI fails if these drift)
& $exe --screenshot docs/images    # re-render the documentation images
& $exe --schema skills/interlude-form/reference/schema.md   # the authoring skill's reference
```

`--schema` matters for the same reason `--docs` does. The authoring skill can only write controls
its reference knows about, so a control added without regenerating it ships a skill that silently
knows the old schema. `SkillTests` fails on the missing discriminator, and CI fails again on the
file itself.

`--screenshot` renders every sample in both themes without showing a window, which makes a
rendering change reviewable: a pull request that alters spacing or contrast can show what it did
rather than describe it. The images are not compared automatically — pixel comparison across
machines, display scales and font versions produces false failures, not confidence.

Wire exactly one thing in a renderer: the control's change event to
`RenderContext.ReportValue`. Never wire one control to another — cross-field behaviour belongs to
`FormSession`, and putting it anywhere else is how a form becomes impossible to reason about.

## Style

The `.editorconfig` covers formatting. Beyond that:

- **Comment the *why*.** The what is in the code. A comment earns its place by recording a
  decision, a trade-off, or a failure mode that is not obvious from reading the lines beneath it.
- **Name tests as sentences.** `A_hidden_required_field_does_not_block_submission` says what the
  system does; `TestValidation3` does not.
- **Prefer a test over a comment** when the thing being protected is behaviour.

## Pull requests

- One idea per pull request.
- Say what changes for someone *using* Interlude, not just what changed in the code.
- Tests for behaviour, and a note on anything you decided not to cover.
- `dotnet test Interlude.sln` and `./scripts/build-all.ps1` both pass.

CI runs on Windows, builds all three Dynamo versions with warnings as errors, runs the tests, and
verifies that each packaged folder contains exactly the two assemblies it should — `Interlude.dll`
and an `Interlude.customization.dll` with no types and no references.

## Reporting bugs

The most useful thing you can attach is the form itself: wire your elements into `Form.Create` and
`Form.ToJson` and paste the result. That makes almost any layout or behaviour problem reproducible
in seconds.

## Licence

By contributing you agree that your contribution is licensed under the BSD 3-Clause licence, as the
rest of the project is.
