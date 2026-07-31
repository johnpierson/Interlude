# RhythmForms — Architecture & Implementation Plan

## Context

Rhythm needs a next-generation forms/UI subsystem for Dynamo: nodes describe a form declaratively, a renderer shows it, and strongly-typed results flow back — replacing the pattern Data-Shapes established, with modern UI, dynamic behaviors (VisibleIf/EnabledIf/RequiredIf, computed values, live validation), theming, and headless testability, with **zero Revit dependencies**.

A spike scaffold already exists on branch `rhythm-forms-spike` (untracked): [src/RhythmForms/RhythmForms.csproj](src/RhythmForms/RhythmForms.csproj), [src/RhythmForms/Directory.Build.props](src/RhythmForms/Directory.Build.props), [scripts/build-all.ps1](scripts/build-all.ps1), [versions.json](versions.json). It builds one assembly **per Dynamo version** (3.0/3.6 → net8.0-windows, 4.0 → net10.0-windows), `UseWPF=true`, `RootNamespace=Rhythm`, single PackageReference `DynamoVisualProgramming.ZeroTouchLibrary` with `ExcludeAssets="runtime"` (loads headless — no DynamoCore/DynamoCoreWpf). No source code yet. **This plan keeps and builds on that scaffold.**

## Decisions confirmed with John

- **UI framework: WPF** (see rationale below)
- **Distribution: bundle into the existing Rhythm deploy** — copy per-Dynamo-version `RhythmForms.dll` into `deploy/<year>` via `deploy/dynamo_to_revit_mapping.json` (2025→3.0, 2026→3.6, 2027→4.0); shipped by the existing view-extension downloader. No standalone package for now.
- **Node naming: grouped facades** — `Input.TextBox`, `Layout.Section`, `Form.Show`, `Condition.*`, `Result.*` under library category `Rhythm.Forms`.
- **V1 scope: full control catalog** (~25 controls, ~10 layout containers).
- **Theming: hand-rolled resource dictionaries** compiled into RhythmForms.dll, seeded by vendoring selected MIT-licensed control templates (e.g. WPF-UI) **as source** — never as a DLL reference.

## 1. UI framework evaluation → WPF (firm)

The deciding constraint: RhythmForms runs **in-process inside someone else's WPF app** (Revit / DynamoSandbox), deployed as a flat bin folder where every extra DLL is hand-placed, CI-unverified, and a version-conflict liability (repo suppresses MSB3277 everywhere; binding-redirect pain is documented history).

- **WinUI 3 / Uno: eliminated.** Bootstrapping Windows App SDK inside Revit's Win32/WPF process is fragile, requires machine-wide runtime installs, and modal ownership over a WPF host is unsupported territory.
- **Avalonia: strong but wrong for v1.** Great theming and `Avalonia.Headless` testing, but costs ~15+ managed DLLs + native Skia/HarfBuzz binaries per deploy folder, a second rendering stack in Revit's memory, self-managed UI thread, and first-load-wins native-DLL clashes if any other package ships a different Avalonia/SkiaSharp. Kept open as a future second renderer behind `IFormRenderer`.
- **WPF: zero deployment DLLs** (in-box on net8.0-windows/net10.0-windows), the host already runs a WPF dispatcher (`System.Windows.Application.Current` in both Revit and Sandbox), trivial modal ownership via `WindowInteropHelper`. The "classic WPF look" problem is bounded — we only style the ~25 controls the renderer emits, not all of WPF.

## 2. Solution & project structure

**Ship exactly ONE DLL: `RhythmForms.dll`.** Layering is enforced by namespaces + an architecture test, not assembly boundaries (each extra assembly multiplies across 3 Dynamo builds × N deploy folders — the repo's known failure mode). Folders mirror namespaces so a future physical split is mechanical.

```
src/RhythmForms/
├── RhythmForms.sln                     (new; RhythmForms + Tests + Preview)
├── RhythmForms.csproj                  SHIPPED — the only deployed assembly (keep existing scaffold)
│   ├── Model/          → Rhythm.Forms.Model          form definition tree (pure POCO, no WPF)
│   ├── Conditions/     → Rhythm.Forms.Conditions     condition/computed-value AST + evaluator
│   ├── Validation/     → Rhythm.Forms.Validation     rule objects + validator
│   ├── Runtime/        → Rhythm.Forms.Runtime        FormSession, state store, dependency graph,
│   │                                                 HostContext, WindowHost, SessionStore
│   ├── Rendering/      → Rhythm.Forms.Rendering      IFormRenderer + registry contracts (no WPF)
│   ├── Rendering/Wpf/  → Rhythm.Forms.Rendering.Wpf  WPF renderer, per-control renderers
│   ├── Theming/        → Rhythm.Forms.Theming        ThemeDefinition (pure model)
│   ├── Themes/         (XAML resource dictionaries — dark + light palettes)
│   ├── Serialization/  → Rhythm.Forms.Serialization  FormDefinition ⇄ JSON (System.Text.Json, in-box)
│   └── Nodes/          → Rhythm.Forms (public)       zero-touch facades: Input, Layout, Behavior,
│                                                     Condition, Compute, Form, Result
├── RhythmForms.Tests/                  DEV-ONLY (net8.0-windows, xunit + Xunit.StaFact)
└── RhythmForms.Preview/                DEV-ONLY WPF exe — form gallery + JSON hot-reload harness
```

Dependency rule (build-breaking via architecture test): `Model/Conditions/Validation/Runtime/Rendering(contracts)/Serialization` may **not** reference `System.Windows.*`. Only `Rendering/Wpf`, `Themes`, and `Runtime.WindowHost` touch WPF.

**Dependency policy: zero runtime dependencies.** BCL + WPF + in-box System.Text.Json only. No Newtonsoft (Dynamo pins its own). ZeroTouchLibrary stays `ExcludeAssets="runtime"`. Post-build check fails if bin output contains anything but `RhythmForms.dll` + `RhythmForms.xml`.

## 3. Object model (`Rhythm.Forms.Model`)

All model types **immutable after construction** (init-only, `IReadOnlyList` children) — nodes re-execute and rebuild the tree each run; nothing to reconcile.

```csharp
FormDefinition { Title, Description, IReadOnlyList<FormElement> Elements,
                 FormButtons Buttons, WindowOptions Window, ThemeDefinition Theme, int SchemaVersion }

abstract FormElement { Key, Label, Tooltip, HelpText,
                       ConditionExpr VisibleIf/EnabledIf/RequiredIf,
                       IReadOnlyList<ValidationRule> Rules, ElementStyle Style }
abstract InputElement : FormElement { object DefaultValue; ComputedValue Computed; }
abstract ContainerElement : FormElement { IReadOnlyList<FormElement> Children; }
```

One **sealed subclass per control** (no ControlType enum — Open/Closed: new control = new class + new renderer registration):

- Inputs: `TextBoxElement` (multiline/placeholder/maxLength), `PasswordElement`, `NumericElement`, `IntegerElement`, `SliderElement`, `DropdownElement` (`OptionItem` value+display+icon), `RadioGroupElement`, `CheckBoxElement`, `ToggleElement`, `ListSelectionElement` (multi + search), `TreeSelectionElement` (recursive `TreeNode`, checkable), `DatePickerElement`, `ColorPickerElement` (own `RgbColor` struct — never `System.Windows.Media.Color`), `FilePickerElement`, `FolderPickerElement`
- Display: `LabelElement`, `MarkdownElement`, `ImageElement` (path/bytes — never BitmapSource), `SeparatorElement`, `SpacerElement`, `ProgressElement`, `ButtonElement` (action: Submit / custom tag / OpenUrl)
- Containers: `VStackElement`, `HStackElement`, `GridElement`, `GroupBoxElement`, `TabsElement`/`TabPage`, `ExpanderElement`, `CardElement`, `ScrollElement`, `DockElement`, `SplitViewElement`

**Keys:** explicit optional `key` parameter on every input node; if empty, derived from label via a frozen, culture-invariant slugify algorithm with `_2/_3` collision suffixing at `Form.Show` time. The slugify algorithm is a versioned API contract.

## 4. Node API (`Nodes/`, category `Rhythm.Forms`)

Repo conventions: public class, private ctor, static methods, XML docs with `<returns name>`, `[MultiReturn]`, optional params (`[DefaultArgument("null")]` for reference-typed optionals). Grouped facades:

- **`Input`**: `TextBox`, `TextArea`, `Password`, `Number`, `Integer`, `Slider`, `CheckBox`, `Toggle`, `DropDown(label, List<object> items, List<string> displayNames, …)`, `ListBox`, `RadioButtons`, `TreeSelect`, `DatePicker`, `ColorPicker`, `FilePath`, `DirectoryPath` — item values are `List<object>` (can be Revit elements/any object); selection returns the object, not its display string.
- **`Layout`**: `Section` (collapsible), `Row`, `Grid`, `Tabs`/`TabPage`, `Expander`, `Card`, `Scroll`, `Dock`, `Split`, `Label`, `Markdown`, `Image`, `Separator`, `Spacer`, `Progress`, `Button`. Containers take `List<FormElement>` — never overload with single-element versions (replication fan-out hazard).
- **`Behavior`**: `VisibleIf(element, condition)`, `EnabledIf`, `RequiredIf`, `Required(element, message)`, `WithValidation(element, rule)`, `WithComputed(element, compute)` — each returns a **new** element (immutability).
- **`Condition`**: `Equals`, `NotEquals`, `GreaterThan`, `LessThan`, `Contains`, `IsEmpty`, `IsChecked`, `In`, `Matches(regex)`, `And(list)`, `Or(list)`, `Not`.
- **`Compute`**: `Format("Hi {name}")`, `Sum(keys)`, `Arithmetic`, `Lookup(map)`, `If(cond, then, else)`.
- **`Form`**:

```csharp
[MultiReturn(new[] { "values", "wasSubmitted", "buttonClicked", "form" })]
public static Dictionary<string, object> Show(
    string title, List<object> elements,
    [DefaultArgument("true")] object trigger,
    string submitText = "Submit", string cancelText = "Cancel",
    double width = 400, double maxHeight = 800,
    string formId = "", bool rememberValues = true,
    bool headlessUseDefaults = false,
    [DefaultArgument("null")] object theme = null,
    [DefaultArgument("null")] object options = null);
```

- **`Result`** (typed accessors so graphs avoid dictionary spelunking): `ValueByKey`, `GetString`, `GetNumber`, `GetBool`, `GetDate`, `GetColor` (hex + `[r,g,b,a]` — no DSCore ref), `GetFilePaths`, `Keys`, `Values`, `WasSubmitted`, `WasCancelled`.

**Stability discipline:** signatures append-only (new optional params at the end only); never rename/reorder/retype; deprecate with `[IsVisibleInDynamoLibrary(false)]`, never delete; `[MultiReturn]` arrays append-only; `AssemblyVersion` frozen `1.0.0.0`, `FileVersion` floats. Enforced by an **API-surface snapshot test** (reflect public `Nodes` surface, diff vs checked-in `api-surface.txt`).

## 5. Conditions & reactive runtime

**Condition-object AST** (not a string mini-language) for v1 — maps 1:1 to Dynamo node composition, trivially serializable/testable; dependency extraction is a tree walk. A string-expression parser compiling to the same AST is a clean v2.

```csharp
abstract ConditionExpr { IEnumerable<string> DependsOn(); bool Evaluate(IFormStateReader state); }
  // Comparison{Key,Op,Operand}, Logical{And/Or/Not, Terms}, AlwaysTrue
abstract ComputedValue { IEnumerable<string> DependsOn(); object Compute(IFormStateReader s); }
```

**`FormSession`** (`Runtime/`) — the heart of "no imperative wiring", fully headless:
- Built once per Show from the definition; walks every VisibleIf/EnabledIf/RequiredIf/Computed/Rule calling `DependsOn()` to build a **dependency graph**; cycles detected at construction (topological sort) and reported before any window opens.
- Renderer calls `SetValue(key, value)` → store updates → dependent conditions/computed/rules re-evaluate in topological order → one coalesced `StateChange` batch → single `Changed` event. Renderer's only job: apply batches.
- `ValidationRule` subclasses: `RequiredRule`, `RangeRule`, `RegexRule`, `LengthRule`, `FileExistsRule`, `CustomPredicateRule`. Live validation = same propagation pass; submit blocked while `ValidateAll()` fails.
- This is where the bulk of the test suite lives: feed values in, assert visibility/errors/computed out, zero UI.

## 6. Renderer contract & theming

```csharp
// Rhythm.Forms.Rendering — no WPF types
interface IFormRenderer { FormResult ShowModal(FormDefinition def, FormSession session); }

// Rhythm.Forms.Rendering.Wpf
interface IControlRenderer {
    Type ElementType { get; }
    FrameworkElement Build(FormElement e, RenderContext ctx);
    void ApplyState(FrameworkElement v, ElementRuntimeState s);
    object ReadValue(FrameworkElement v);
    void WriteValue(FrameworkElement v, object value);
}
sealed class ControlRendererRegistry { Register(IControlRenderer); Resolve(FormElement); }
```

- Registry resolves by element type walking the hierarchy; **unknown element → FallbackRenderer** (placeholder, not a crash). New control = element class + renderer + `Register` — renderer core untouched (Open/Closed). Custom containers recurse via `ctx.BuildChild`.
- Each control wires exactly one thing: its change event → `session.SetValue`. The window holds the single `session.Changed` subscription. No control-to-control wiring anywhere.
- **Theming:** `ThemeDefinition` (accent, palettes, corner radius, fonts, density, Dark/Light/Auto) → `WpfThemeApplier` injects brushes as dynamic resources into the **window's own `Resources`** — **never `Application.Current.Resources`** (hard rule; we're a guest in Revit's/Dynamo's Application). `Themes/*.xaml` consume only `{DynamicResource RhythmForms.*}` keys → runtime dark/light toggle is a resource swap. Vendored MIT templates (WPF-UI etc.) come in as source with attribution.

## 7. Execution semantics & threading

**Show algorithm** (`Runtime/WindowHost`):
1. `dispatcher = System.Windows.Application.Current?.Dispatcher`
2. null → headless path: `headlessUseDefaults:false` (default) throws a clear `InvalidOperationException` (surfaces as node warning); `true` returns defaults dict with `wasSubmitted=false`. Headless detection = `Application.Current == null` + `Environment.UserInteractive` + process-name list (`DynamoCLI`, `DynamoWPFCLI`, GD executive) in one `HostContext` class.
3. `dispatcher.CheckAccess()` → show directly (Revit: scheduler runs on Revit's UI context — Data-Shapes' model)
4. else → `dispatcher.Invoke(() => BuildAndShow(...))` (Sandbox: background scheduler thread; graph blocks while UI pumps). Never spin our own STA thread when a host dispatcher exists.

**Window ownership:** `GetActiveWindow()` P/Invoke → fallback `Process.MainWindowHandle` → `WindowInteropHelper.Owner`; manual center-on-owner math; `ShowInTaskbar=false`, `Topmost=false`. Never `EnableWindow(false)` on Revit's frame.

**Re-execution reality** (documented, not fought — upstream changes re-pop the dialog):
- `trigger` input: exactly `false` → skip UI, return cached result for `formId` (or defaults). Doubles as a sequencing input.
- **Re-entrancy latch** per `formId` (`Interlocked`): second concurrent Show blocks on the first window's result instead of stacking modals (Automatic-mode dialog storms).
- **`SessionStore`**: process-lifetime `ConcurrentDictionary<string, FormResult>` keyed by `formId` (or hash of title + ordered keys); `rememberValues:true` pre-fills previous submission. Cancel does **not** overwrite the cache. Seam (`IResultStore`) for opt-in disk persistence later.
- **Cancellation contract:** cancel/X returns **defaults for every key** (never null — Data-Shapes' null-on-cancel is a constant support burden), `wasSubmitted=false`, `form.WasCancelled=true`.
- Statics limited to the latch + SessionStore; cleared on `Dynamo.Events.ExecutionEvents.GraphPostExecution` where appropriate (DynamoServices, already transitively referenced).

## 8. Deployment & CI (bundled into Rhythm deploy — per John's choice)

- Build matrix stays as scaffolded: `scripts/build-all.ps1` × `versions.json` active list (3.0, 3.6, 4.0).
- Copy into deploy via `deploy/dynamo_to_revit_mapping.json`: `deploy/2025` ← 3.0 build, `deploy/2026` ← 3.6 build, `deploy/2027` ← 4.0 build. Files per folder: `RhythmForms.dll`, `RhythmForms.xml` (required for port names/tooltips), `RhythmForms_DynamoCustomization.xml`.
- Add `"RhythmForms, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"` to `deploy/pkg.json` `node_libraries`; add RhythmForms to the view-extension download list in [src/RhythmViewExtension/RhythmViewExtension.cs](src/RhythmViewExtension/RhythmViewExtension.cs) / `Global.cs` so it actually reaches users.
- **CI** ([.github/workflows/build.yml](.github/workflows/build.yml)): add `setup-dotnet` with `8.0.x` **and** `10.0.x`; run `build-all.ps1` via `dotnet build` (keep out of the msbuild .sln steps); verify `RhythmForms.dll` + `.xml` per version **and that no other DLLs appear**; run tests incl. API-surface snapshot + architecture test; copy to deploy folders.
- **Icons:** ship `RhythmForms.customization.dll` (resx+AL, same as RhythmCore) for the net8 rows; spike whether that same resource-only assembly loads fine on the net10/2027 row (resource assemblies have no real TFM constraint — if it works, it also fixes RhythmCore's 2027 icon gap). Ship 2027 without icons if the spike fails; don't block v1.
- Older Revit years (2020–2024, net48/Dynamo 2.x): **out of scope** — RhythmForms is net8+/Dynamo 3.0+ only, consistent with `versions.json`.

## 9. Pitfalls register (design-time mitigations baked in above)

1. Automatic-mode dialog storms → trigger gate + re-entrancy latch + docs ("use Manual run").
2. Results feeding back upstream → oscillation; docs + definition-hash caching within one evaluation.
3. Host resource pollution → window-scoped resources only; test under Revit dark theme.
4. Assembly conflicts → zero-dependency rule + CI "no extra DLLs" check; keep System.Text.Json in-box (no NuGet package).
5. Culture → invariant parsing at model boundary, `CurrentUICulture` rendering; explicit `de-DE` tests.
6. WPF leaks → no `DependencyPropertyDescriptor.AddValueChanged`; unsubscribe `session.Changed` on `Window.Closed`; per-Show instances everywhere.
7. Data-Shapes coexistence → no `UI.*` class names; distinct namespaces; publish a Data-Shapes→RhythmForms mapping doc (call out the cancel-returns-defaults delta loudly).
8. Duplicate-assembly loads (if a standalone package ever ships later) → identical assembly identity per release.
9. `ExcludeAssets=runtime` trap → Tests/Preview must never execute Dynamo types; attributes as metadata are safe.

## 10. Improvements over Data-Shapes / 5-year evolution

**Improvements:** typed dictionary results + accessor nodes (vs positional `object[]`); declarative VisibleIf/RequiredIf/computed values; live validation; real layout system (grid/tabs/split vs single stack); theming/dark mode/branding; defaults-not-nulls on cancel; unknown-control fallback; headless-testable core; open registry extensibility; one documented threading model; headless/GD-aware behavior.

**Evolution:** Y1 — JSON schema (`SchemaVersion` + `$type` discriminators) as the strategic contract: forms become shareable, diffable data; Preview app consumes it. Y1–2 — `HeadlessFormRenderer` answering forms from a supplied dictionary (Player/batch/CI story). Y2 — string expression language compiling to the existing AST. Y2–3 — public registry docs so other packages register element+renderer pairs (e.g. Revit-aware pickers living in RhythmRevit — dependency arrow stays correct). Y3+ — second renderer (Avalonia or web/JSON→HTML) behind `IFormRenderer` if non-Windows hosts materialize. Retire hand-rolled theme progressively when .NET's Fluent WPF theme covers all supported TFMs.

## 11. Implementation phases (one project-area at a time, as agreed)

1. **Model + Conditions + Validation + Runtime (headless)** — FormDefinition tree, AST, FormSession/dependency graph, SessionStore, HostContext; tests first-class. No UI yet.
2. **Serialization** — JSON round-trip + schema version; architecture test wired in.
3. **WPF renderer + theming** — registry, per-control renderers for full catalog, Themes/*.xaml dark+light, WindowHost.
4. **Preview harness** — gallery + JSON hot-reload; iterate visuals here.
5. **Nodes facade** — full grouped API, XML docs, `_DynamoCustomization.xml`, API-surface snapshot test.
6. **Packaging + CI** — build.yml job, deploy-folder copies, pkg.json, view-extension download entries, customization.dll icon spike.
7. **Host matrix validation** — Revit 2025/2026/2027 (manual + automatic run), Sandbox, Dynamo Player, one GD headless run.

## 12. Verification

- **Unit (headless):** session propagation (visibility/enabled/required/computed cascades, cycle detection), validation rules, slugify + collisions, cancellation contract, SessionStore pre-fill, HostContext detection (injected probes), JSON round-trip, architecture layering test, API-surface snapshot.
- **STA smoke tests:** render every element type from a definition, assert one control per element, set values programmatically, submit, assert result dict. No pixel tests.
- **Preview app:** visual QA for both themes, all controls/layouts, reduced-motion.
- **Manual matrix:** the phase-7 checklist above; verify z-order over Revit, dark theme, `de-DE` locale, cancel/re-run pre-fill, trigger gate, GD throws cleanly.