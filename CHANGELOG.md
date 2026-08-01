# Changelog

All notable changes to Interlude are recorded here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

One version rule worth stating up front: **`AssemblyVersion` is frozen at `1.0.0.0` and will not
change.** Saved graphs bind to it, and moving it would break every one of them on upgrade. The
package version and `FileVersion` are what move.

## [Unreleased]

### Changed

- **The default form design is now neubrutalist, in light mode.** A form with nothing wired into
  its theme port gets heavy black outlines, square corners, solid unblurred shadows offset down and
  to the right, flat loud colour, and type set hard. Buttons drop onto their own shadow when
  pressed.

  It no longer follows the Windows light/dark setting. The palette is built around cream and black,
  and flipping to the inverted one because the machine happens to be set to dark is a different
  design rather than the same one dimmed. `Theme.System` is now the same look with `mode` set to
  `Auto`, for a graph that would rather match the machine.

  A default that looks like nothing in particular is a decision too, and it is the wrong one for a
  package whose whole job is the dialog. `Theme.Light` and `Theme.Dark` are unchanged and remain
  the conventional look — hairline outlines, rounded corners, no shadows — so the quiet option is
  one node away rather than unavailable.

- **The slider, the date field and the progress bar are now drawn by Interlude** rather than left
  to Windows. They were the three controls that ignored the theme's outline, corner shape and
  shadow, and one stock hairline control among a page of heavy outlines is the single thing that
  makes a themed form look half-finished. The date field's drop-down calendar keeps Windows' own
  template, coloured to match.

- **A toggle switch that is off no longer uses the theme's alternate surface colour.** A theme is
  free to make that colour loud — the neubrutalist one makes it yellow — and a bright yellow switch
  reads as *on* however the knob is placed.

### Added

- **Node help inside the package.** Every one of the 112 nodes now ships a Markdown help page in
  the package's `doc/` folder, so selecting a node in Dynamo and opening Help shows Interlude's own
  documentation in the panel beside the graph instead of "no documentation available". The format
  is Dynamo's own, taken from the fallback docs that ship with Dynamo Core.

  The pages are generated from the shipped assembly — signatures, port names, types and defaults by
  reflection, prose from the XML documentation file the compiler emits beside the DLL. That is the
  same file Dynamo reads for its port tooltips, so the help panel and the tooltip cannot say
  different things, and changing what a node's help says means changing the `///` comment on the
  node. A test fails when a node has no page; CI regenerates the folder and fails on any
  difference.

- **`Theme.Neubrutalism`** — the default preset, by name, so a graph can ask for it explicitly and
  choose light or dark rather than following Windows.
- **`Theme.Create` gained `borderWidth`, `shadowOffset` and `heavyText`** (appended, as the rules
  require). `shadowOffset` is a solid, unblurred offset — set it to zero for no shadow. A card that
  asks for a shadow with `Layout.Card` still gets a soft one in themes that do not offset shadows,
  so the two ideas do not collide.
- **`Theme.Mono`** — a monochrome preset: black, white and grey, pill-shaped controls, and small
  spaced capitals for headings. Errors keep a red, deliberately: an error nobody can pick out from
  ordinary text is a usability bug, and no amount of restraint is worth that.
- **Space Grotesk is the default font**, embedded inside `Interlude.dll` rather than named, so a
  form renders the same everywhere instead of depending on what the machine has installed. It
  replaces Comic Neue, which was the wrong flavour of playful for this style: neubrutalism is set
  in heavy grotesques, and Comic Neue has no weight that can hold its own beside a three-pixel
  outline. Override it on `Theme.Create`'s `fontFamily` port. SIL Open Font Licensed; see
  [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

  Four *static* faces are embedded — Light, Regular, Medium, Bold — rather than the variable font
  Google Fonts distributes. WPF has no support for variable font axes: it renders the default
  instance and synthesises the rest, which would give a smeared algorithmic bold everywhere
  `heavyText` asks for weight.
- **`Theme.Create` gained `shape`, `uppercaseHeaders` and `headerTracking`** (appended, as the
  rules require). `shape: "Pill"` derives its radius from the control height rather than from
  `cornerRadius`, because "fully rounded" depends on how tall a control is.
- **`Layout.Progress` gained `segments`** — discrete cells instead of a continuous fill, for
  counting rather than measuring.

### Fixed

- **A built-in palette is now serialised by name.** A form's JSON carried all eighteen colours of
  both palettes whenever the theme did not use the stock light and dark ones — three hundred lines
  in front of a two-field form, and a palette diff in every checked-in form each time the built-in
  colours were tuned. Themes now record which preset they started from (`"preset":
  "neubrutalist"`); a palette is written out only when an author actually replaced one.

- **A "System" category appeared in the library beside "Interlude".** Dynamo imports the base
  types and signature types of every public type, so exceptions deriving from
  `InvalidOperationException` dragged in `Exception` and `SystemException`, the WPF renderer
  dragged in `System.Windows`, and the JSON converter dragged in `System.Text.Json`.
  `[IsVisibleInDynamoLibrary(false)]` cannot help — it hides *our* type, not the framework type
  behind it. The rendering layer, the exceptions, the live-state types and the JSON converter are
  now internal, and a test reads the importer's own output to keep it that way.

- **No nodes appeared in Dynamo at all.** `RgbColor` had a constructor whose `alpha` parameter
  defaulted to `255` — a `byte`. Dynamo imports a zero-touch assembly by building a DesignScript
  AST for every public constructor in it, and its `AstFactory` has no case for `byte`, so the
  import threw `LibraryLoadFailedException` and the whole package failed to load. The opacity is
  now a separate constructor overload with no default.

  Guarded three ways so it cannot recur: every public member is checked against the importer's
  rules, and Dynamo's *real* importer is now run over the built assembly on every build.

## [1.0.0]

First release.

### Added

**The node library**, under a single `Interlude` category:

- `Input` — 17 fields: text, multi-line text, password, number, integer, slider, check box,
  toggle, dropdown, list, radio buttons, tree, date, colour, file path, folder path.
  Choice inputs return the objects that were put in, not their display names.
- `Layout` — sections, rows, columns, grids, tabs, cards, expanders, splitters, dock panels,
  scrolling regions, plus labels, Markdown, images, separators, spacers, progress and buttons.
- `Behavior` — `VisibleIf`, `EnabledIf`, `RequiredIf`, `Required`, `WithValidation`,
  `WithComputed`, `WithKey`, `WithHelp`, `WithSize`, `ReadOnly`. Each returns a new element.
- `Condition` — comparisons, membership, emptiness, regular expressions, and `And` / `Or` / `Not`.
- `Compute` — `Format`, `Sum`, `Arithmetic`, `Lookup`, `If`, `Field`, `Constant`.
- `Rule` — `Required`, `Range`, `Regex`, `Length`, `FileExists`, `FolderExists`, `CompareTo`.
- `Theme` — `Light`, `Dark`, `System`, `Create`, `WithColors`.
- `Form` — `Show`, `ShowDefinition`, `Create`, `Options`, `Check`, `ToJson`, `FromJson`, `Forget`.
- `Result` — `GetString`, `GetNumber`, `GetInteger`, `GetBool`, `GetDate`, `GetColor`, `GetList`,
  `GetFilePaths`, `Keys`, `Values`, `WasSubmitted`, `WasCancelled`, `ButtonClicked`, `HasKey`.

**Behaviour**

- Declarative visibility, enablement and requirement, evaluated from the form's own answers.
- Computed values, recalculated in dependency order. Loops between them are rejected when the
  form is built, before a window appears.
- Live validation, with rules that can read other fields.
- Hidden fields are never validated and never block submission.
- Cancelling returns every field's default value, never nulls, with `wasSubmitted` false.
- A `trigger` input that skips the dialog and returns the last answers.
- A re-entrancy latch: a form already on screen is never opened twice.
- Remembered answers per form, which cancelling never overwrites.
- A clear, explanatory error when no user interface is available — or defaults, if opted into.
- Unknown controls render as a visible placeholder rather than taking the form down.

**Presentation**

- Hand-rolled light and dark themes, following the Windows app theme by default.
- Accent colour, density, corner radius, font and label-column width.
- Everything scoped to the form's own window; the host application's resources are never touched.

**Forms as documents**

- Lossless JSON round-trip with a schema version and `$type` discriminators.
- Worked examples in [`samples/`](samples/), validated against the schema on every build.

**Distribution**

- One assembly per Dynamo version: 3.0 and 3.6 on `net8.0-windows`, 4.0 on `net10.0-windows`.
- Zero runtime dependencies. The build fails if a second assembly appears in the output.

**Development**

- A preview harness with a sample gallery, theme controls and JSON hot reload.
- Architecture tests for the layering, library visibility, renderer coverage and schema coverage.
- An API-surface snapshot, because saved graphs bind to node names and parameter positions.

[Unreleased]: https://github.com/johnpierson/Interlude/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/johnpierson/Interlude/releases/tag/v1.0.0
