# Architecture

How Interlude is put together, and why. This is the document to read before changing anything
structural; the reasoning matters more than the shapes.

## The one idea

**A form is a value.** Nodes build an immutable tree, a session evaluates it, a renderer draws it.
Everything else follows from that.

```
Nodes ──build──► FormDefinition ──feeds──► FormSession ──batches──► Renderer
 (Interlude)      (immutable data)         (live state)             (WPF today)
                        │                        │
                        └──── JSON ◄─────────────┘
```

Because the definition is data rather than a pile of controls, it serializes, diffs, replays in a
test, and could be drawn by something that is not WPF. Because the session owns all the
behaviour, the interesting tests need no UI thread at all.

## Layers

One code assembly, layered by namespace. Enforced by
[`ArchitectureTests`](../tests/Interlude.Tests/ArchitectureTests.cs), not by project references.

| Namespace | What lives there | WPF? |
| --- | --- | --- |
| `Interlude.Model` | The form tree: elements, options, keys, colours, spacing | no |
| `Interlude.Conditions` | Condition and computed-value AST, value coercion | no |
| `Interlude.Validation` | Rule objects | no |
| `Interlude.Runtime` | `FormSession`, dependency graph, result store, latch, host detection, the Revit selection bridge | no |
| `Interlude.Serialization` | JSON in and out | no |
| `Interlude.Theming` | `ThemeDefinition`, palettes, resource key names | no |
| `Interlude.Rendering` | `IFormRenderer` — the renderer contract | no |
| `Interlude.Rendering.Wpf` | The WPF renderer, controls, window, threading | yes |
| `Interlude` | The node facades | no |

### Why one assembly

Layering by namespace rather than by project is a deliberate trade. Each extra assembly would
multiply across three Dynamo builds and every package folder, and each one lands in a flat
directory that Revit shares with every other add-in. A wrong reference caught by a test costs a
build; a second DLL shipped to every user costs someone else's Revit install.

The folders mirror the namespaces, so if a physical split ever becomes worth it, it is mechanical.

### The one exception: node icons

A package folder contains a second file ending in `.dll`, and it is worth being precise about why
the rule bent for it and would not bend again.

Dynamo will not read node icons out of the node library. It looks for a sibling assembly named
`<AssemblyName>.customization.dll` and pulls PNGs from a resource stream inside it. There is no
attribute, no folder convention and no manifest entry that does the same job — it is that file or
Dynamo's default cube on all 115 nodes.

What makes it affordable is that [`Interlude.customization.dll`](../src/Interlude.Icons) contains
no types and references nothing but the `netstandard` facade. The rule was never really about file
count; it was about what a file can collide with. A DLL with no types cannot be bound against,
cannot export a version another package disagrees with, and cannot execute. It is a zip of
pictures with a `.dll` extension because that is the container Dynamo happens to read.

So the rule is now: **one code assembly, plus one resource assembly that is checked to be inert.**
That check is real and runs three times — `VerifyResourceOnly` in the project, a type count in
`build-all.ps1`, and a types-and-references check over the packed folders in CI. The moment the
icon assembly acquires a type or a package reference, it stops being a picture container and
starts being the dependency the rule exists to keep out, and all three fail.

### How the icons are made

115 nodes at two sizes is 230 images, and they are drawn rather than hand-authored — by
[`Icons.cs`](../tools/Interlude.Preview/Icons.cs) in the preview harness, using the same offline
WPF rendering that produces the documentation screenshots.

They use a **family system**: the plate colour identifies one of the nine categories, and a shared
vocabulary of about eighty glyphs says what the node does. Glyphs are reused across categories on
purpose — a calendar is a calendar whether it is `Input.DatePicker`, `Rule.Range` on a date, or
`Result.GetDate`. In the library tree an icon is drawn at roughly sixteen pixels, which is not
enough interior to separate `Condition.GreaterThan` from `Condition.AtLeast` however carefully
they are drawn; colour answers "which family", shape answers "what kind of thing", and the label
beside the icon does the fine distinguishing it is already there to do.

The PNGs and the compiled `InterludeImages.resources` container are both checked in, and CI
regenerates and diffs them like it does the samples and the node help. The container is written by
`ResourceWriter` rather than compiled from a `.resx`, because MSBuild cannot produce the format
Dynamo reads — see the comment in [`Icons.cs`](../tools/Interlude.Preview/Icons.cs), which records
what that cost to find out.

## The reactive session

[`FormSession`](../src/Interlude/Runtime/FormSession.cs) is where the package earns its keep.

At construction it walks every `VisibleIf`, `EnabledIf`, `RequiredIf`, computed value and rule,
asks each what it `DependsOn()`, and topologically sorts the computed values. **Cycles are found
here** — before a window exists — because the alternative is a dialog that opens and then hangs.

From then on the contract is one-way and tiny:

```
control changes ──► session.SetValue(key, value)
                         │
                         ├─ recompute computed values, in dependency order
                         ├─ recompute visibility / enablement / required, parents before children
                         ├─ recompute validation
                         │
                         └──► ONE Changed event carrying the whole batch
                                       │
                                       └──► renderer applies it
```

Controls never talk to each other. A control's entire outward contract is "my value changed", and
every consequence arrives as a batch. That is what makes the renderer's job "apply batches" and
nothing else, and it is why a change cascading through four fields still repaints once.

### Why every pass recomputes everything

`Propagate` re-evaluates every computed value, condition and rule on every edit, rather than
tracking which ones a particular change could have touched.

Forms are tens of fields, not millions of cells. The full pass costs nothing measurable, and it
removes the entire class of bug where an incremental update misses a dependency and a field goes
stale in a way that only reproduces on someone else's machine. The dependency graph still matters
— it fixes the *order* computed values are evaluated in, and it catches cycles — but it is not
load-bearing for correctness of the sweep.

If a form ever appears with enough fields for this to matter, the graph is already there to make
the pass incremental. It has not been needed.

### Hidden fields

A hidden field is not validated and never required. This is not an optimisation: a required
field the user cannot see blocks submission with no control to fix it, which is the single
fastest way to make a conditional form unusable.

Its value still appears in the results, so a downstream node reading it by name always finds it.

## Immutability

Every model type is a `record` with `init`-only members.

Dynamo re-executes a graph from scratch on every change, so the tree is rebuilt rather than
reconciled — there is no mutable state to get out of sync. Records give `with` expressions, which
is what lets `Behavior.VisibleIf` return a modified copy of *any* element without a visitor per
element type, preserving the concrete type for free.

The session holds the only mutable state, and it is per-showing.

## Keys

Every answer is addressed by a key. Given explicitly, it is used as given; left empty, it is
derived from the label by [`FormKeys.Slugify`](../src/Interlude/Model/FormKeys.cs).

That algorithm is a **versioned API contract**, not an implementation detail. A saved graph reads
`values["wall_type"]`, and it keeps working only for as long as "Wall Type" keeps slugifying to
`wall_type`. Changing the rules means bumping `SlugVersion` and treating it as breaking.
Collisions get `_2`, `_3` suffixes in document order, so adding a field at the bottom of a form
never renumbers the fields above it.

## Rendering

```csharp
interface IControlRenderer {
    Type ElementType { get; }
    bool UsesFieldChrome { get; }
    FrameworkElement Build(FormElement element, RenderContext context);
    void ApplyState(FrameworkElement control, ElementRuntimeState state);
    object? ReadValue(FrameworkElement control);
    void WriteValue(FrameworkElement control, object? value);
}
```

Adding a control is: a sealed element record, a renderer, a line in
`ControlRendererRegistry.CreateDefault`, and a `[JsonDerivedType]`. The renderer core is untouched.
Two architecture tests fail if you forget either of the last two.

Resolution walks up the type hierarchy, so a subclassed element inherits its base's renderer. An
element with no renderer at all draws a **visible placeholder** rather than throwing: a form
containing one control this build has never heard of is still worth showing, and throwing would
turn "this graph needs a newer Interlude" into "this graph is broken".

Label, help text, required marker and error line are drawn once, by
[`FieldChrome`](../src/Interlude/Rendering/Wpf/FieldChrome.cs). That is what makes twenty
different controls look like one form, and what gives a new control correct labelling for free.

## Theming

`ThemeDefinition` is pure data. `WpfThemeApplier` turns it into brushes and injects them into the
**form window's own `Resources`**.

Never `Application.Current.Resources`. Interlude runs inside Revit and inside Dynamo — someone
else's application, with someone else's styling — and writing to the application dictionary would
restyle their UI from underneath them. There is a test for it.

The XAML in `Themes/` consumes only `{DynamicResource Interlude.*}` keys, so switching light to
dark is a dictionary update rather than a rebuild.

That leaves the two halves joined by nothing but a string, and a mismatch is silent: an unresolved
dynamic lookup leaves the property at its default, so a typo'd key renders as merely plain rather
than broken. [`ThemeResourceTests`](../tests/Interlude.Tests/ThemeResourceTests.cs) scans the XAML
for the keys it asks for and checks each one against `ThemeKeys` and against what the applier
actually writes.

One key is *deliberately* left unwritten. `Interlude.ControlShadow` exists only when a theme has a
shadow offset; a theme without one removes the key, and the unresolved lookup leaves `Effect` null.
Writing an invisible effect instead would give every control in every form a render layer it could
not see, so the absence is load bearing and there is a test holding it in place.

Two shadows, not one, because they answer different questions. `Interlude.ControlShadow` is the
theme's own — hard, unblurred, offset — and applies to everything. `Interlude.CardShadow` is what
a card gets when *the card* asks for one with `Layout.Card`, and it is always present: hard when
the theme offsets shadows, soft and blurred when it does not.

Error styling is driven by an *inherited* attached property, `FieldState.HasError`. The renderer
flags the control it holds; the theme decides which part of a composite control turns red. That
way the renderer needs no knowledge of any control's visual tree.

## Threading

[`WindowHost`](../src/Interlude/Rendering/Wpf/WindowHost.cs) handles three hosts that behave
differently:

| Host | Scheduler runs on | What happens |
| --- | --- | --- |
| Revit | Revit's UI thread | Show directly |
| Dynamo Sandbox | A background thread | `dispatcher.Invoke` — the graph blocks while the dialog pumps, which is what a modal question means |
| Command line, scheduled run | No dispatcher at all | Throw with an explanation, or return defaults if `headlessUseDefaults` was set |

**Interlude never creates its own STA thread when a host dispatcher exists.** A second UI thread
inside Revit produces a dialog the host cannot own, cannot order correctly, and cannot reliably
close.

The dialog is *owned* to the host window via `WindowInteropHelper` rather than made `Topmost`: an
owned window stays above Revit without floating above unrelated applications. Centring is done by
hand because `CenterOwner` only works for a WPF owner and Revit's main window is Win32.

## The Revit selection bridge

`Input.SelectElements` lets a form send the user into the Revit model to pick, and it does so
without a Revit reference. [`RevitSelectionBridge`](../src/Interlude/Runtime/RevitSelectionBridge.cs)
reaches everything by reflection over assemblies already loaded in the process: `RevitServices`
for the active document, `RevitAPIUI` for `PickObject`/`PickObjects`, and `RevitNodes` to wrap
what came back as the `Revit.Elements.Element` every downstream node expects. A compile-time
reference would multiply the build matrix by every Revit version and end the zero-dependency
rule, for three method calls; outside Revit the probe finds nothing, says why, and the control
renders disabled instead of the assembly failing to import.

The pick is allowed to run because of the threading table above: in Revit the form is shown
directly on Revit's UI thread, inside the API context Dynamo evaluates in, and `ShowDialog`'s
nested message loop stays inside it. Stepping the modal form aside is the delicate part, and both
halves are load-bearing: the window is **minimised, never hidden** — hiding a window shown with
`ShowDialog` ends the modal session — and every top-level window on the thread that `ShowDialog`
disabled is re-enabled for exactly the duration of the pick, then re-disabled in a `finally`.
*Every* window, not the owner: WPF's modal loop disables the whole thread's windows — Revit's
frame and Dynamo's alike — so re-enabling just the recorded owner leaves a model that shows the
picking prompt but cannot take a click.

Cancelling a pick (Escape) keeps the previous answer, spotted by the exception's type *name*,
since `Autodesk.Revit.Exceptions.OperationCanceledException` is not a type Interlude can name.
The reflection path itself only runs inside Revit, so the tests pin everything around it — the
honest unavailability, the coercion shapes, the control's behaviour through a fake picker — and
the real path is checked by running the sample graph in Revit, recorded in
[installing.md](installing.md#where-it-has-been-run).

## Culture

Parsing and storage are invariant; display is the user's culture.

Values arrive from Dynamo, from JSON and from text boxes typed on machines set to any locale. If
parsing followed the current culture, `"1,5"` would mean 1.5 on one machine and 15 on another and
a saved form would stop round-tripping. So `ValueOps` and `FormJson` are invariant throughout.

The inversion is at the edges: a numeric field displays and parses in
`CultureInfo.CurrentCulture`, because someone typing "1,5" on a German machine means one and a
half — and it hands the session a `double`, which has no culture at all.

Turkish is the case that catches naive slug code: its lowercase `I` is a dotless `ı`, so a
culture-sensitive `ToLower` would slug "Wall ID" to something no ordinal lookup would ever match.
There is a test.

## Execution semantics

| Concern | Answer |
| --- | --- |
| Graph re-runs and re-opens the dialog | `trigger: false` skips it and returns the last answers |
| Automatic mode opens several at once | Re-entrancy latch per form: the second caller waits for the first window's result |
| Answers lost between runs | `SessionStore`, keyed by `formId` or a hash of title plus ordered keys |
| Cancelling destroys remembered answers | It does not — only a submitted result is stored |
| Cancelling returns nulls | It returns every field's default, and says `wasSubmitted: false` |
| No UI available | A clear exception, or defaults if explicitly opted into |

## Dependencies

BCL, in-box WPF, in-box `System.Text.Json`. That is the whole list.

No Newtonsoft — Dynamo pins its own version and fighting it is the exact class of problem this
package exists to avoid. The Dynamo reference is `ExcludeAssets="runtime"`: it supplies attributes
at compile time and is never copied.

One consequence worth knowing: because Interlude's public types carry
`[IsVisibleInDynamoLibrary]`, .NET must resolve `DynamoServices.dll` whenever anything reflects
over them — which `Enum.GetNames` and `System.Text.Json`'s enum handling both do. Dynamo always
has it. A standalone host needs to reference it, which is why the preview harness does.

## Testing

| Kind | What it covers |
| --- | --- |
| Headless | Session propagation, cycle detection, validation, slugify, cancellation, the store, the latch, host detection, JSON round-trip |
| STA smoke | Every element builds a control, theme resources resolve, state reaches controls, no host resources touched |
| Architecture | Layering, library visibility, one-assembly rule, renderer and schema coverage |
| API surface | The node API against a checked-in snapshot |
| Samples | Every example form parses, round-trips and renders |

No pixel tests. What matters is that the tree is built, state reaches the controls and edits reach
the session — not that a border is two pixels wide. That is what the preview harness is for.
