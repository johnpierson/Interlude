# Changelog

All notable changes to Interlude are recorded here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

One version rule worth stating up front: **`AssemblyVersion` is frozen at `1.0.0.0` and will not
change.** Saved graphs bind to it, and moving it would break every one of them on upgrade. The
package version and `FileVersion` are what move.

## [Unreleased]

### Fixed

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
