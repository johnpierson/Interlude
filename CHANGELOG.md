# Changelog

All notable changes to Interlude are recorded here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

One version rule worth stating up front: **`AssemblyVersion` is frozen at `1.0.0.0` and will not
change.** Saved graphs bind to it, and moving it would break every one of them on upgrade. The
package version and `FileVersion` are what move.

## [Unreleased]

### Added

- **`Layout.Preview` — a value the form works out and shows back, live, as its inputs are edited.**

  ```
  Layout.Preview("New name", "{prefix}{sample_name}{suffix}")
  ```

  This was already possible, by putting a computed value on a read-only text box, and that is what
  people were doing. It cost three nodes, put an answer nobody gave into the results dictionary,
  and drew a text box the user would try to type into. A preview has no key, never appears in
  `values`, is never validated and is not a tab stop. The text is selectable, because the thing
  people most often want from a previewed name is to paste it somewhere.

  A preview can only read what is already on the form — Interlude has no notion of the fifty
  elements a graph is about to rename, so the author puts one sample on the form as an ordinary
  field with a default. That field being editable turns out to be the feature: it is how someone
  tries the rule against the worst name in the model before committing to it.

- **Format specifiers in templates.** `{sequence:000}` pads to three digits, `{total:F2}` fixes two
  decimals, `{due:yyyy-MM-dd}` writes a date the way a file name wants it. Any .NET specifier works
  after the colon; field keys are slugs and never contain one, so the first colon always separates
  the two. Without a specifier a number prints the shortest form that round-trips, which is why a
  total of 546.0 used to read `546` and a sequence starting at 1 could not read `001`.

- **A shorthand for computed values in JSON.** A bare scalar may stand in for the object: a string
  with a brace in it is a template, a string without one is a field key, and a number or boolean is
  a constant.

  ```json
  "value": "{prefix}{sampleName}"
  "ifTrue": "{prefix}{sampleName} {startNumber:000}"
  "left": "quantity"
  ```

  The brace rule is the one the nodes have always followed — `Compute.Arithmetic("quantity",
  "Multiply", "unitPrice")` has meant the fields since the first release — so a string now reads
  the same way on a port and in the file that port's graph saved. `Form.ToJson` still writes the
  long form, which keeps a form written by this release readable by every earlier one.

### Changed

- **`Compute.If` and `Compute.Arithmetic` accept a template directly.** A bare string containing a
  brace is now read as a format template rather than as the key of a field that does not exist, so
  `Compute.If(c, "{prefix}{name}", "{name}")` no longer needs `Compute.Format` around each branch.
  A string with no brace is still a field key.

### Fixed

- **`requiredIf` on an element that collects nothing no longer blocks submission for ever.** A
  label, a container or a preview has no value and never will, so a required one could not be
  satisfied — and reported no error against any field, because there was no field to report it
  against. Only elements that produce a value can now be required.

## [1.0.3] - 2026-08-03

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

- **A form can be written from a description of it.** `skills/interlude-form` is a Claude Code
  skill that turns "ask for a prefix, a check box for sheets, and hide the folder picker unless
  it's ticked" into a form file, and checks it before handing it over.

  It ships as a **separate download**, `Interlude-skill-<version>.zip`, unzipped into
  `~/.claude/skills`. Nothing in it goes into a Dynamo package: the "exactly one code assembly"
  rule is about the folder Revit shares with every other add-in, and this is not that folder.

  The schema reference behind it is generated from the assembly rather than written, because a
  hand-written list of controls is correct on the day it is written and a skill cannot emit a
  control it has never heard of. CI regenerates it and fails if the checked-in copy has moved, so
  adding a control to Interlude cannot ship a skill that still knows the old schema.

- **`interlude-check`, a command-line form checker.** Reads form JSON and reports what is wrong
  with it: anything the reader would refuse, plus conditions naming fields that do not exist and
  computed values that depend on each other in a loop. It calls the same `Form.Check` node a graph
  would, rather than reimplementing the checks — a checker that disagrees with the node is worse
  than none.

  Exits non-zero, so it belongs in a build. It travels with the skill; it is the first thing
  Interlude has ever shipped that is not the package.

- **`Form.WithOptions`, which fills in a choice field of a form that already exists.** It takes a
  form, the key of a drop-down, radio group or list box, and the items to put in it, and returns a
  new form with that field's options replaced.

  It closes the one hole in the forms-as-documents story. A form checked into a repository cannot
  carry Revit elements — they do not exist in another model, and saving them writes their names and
  says so in the file — so the only way to ask "which levels?" from a shared form was to stop using
  the file and rebuild the whole thing in the graph. Now the file holds the layout, the labels, the
  conditions and the validation, and the graph supplies the one thing only the open model knows:

  ```
  Form.FromJson ──► Form.WithOptions(key: "levels", items: levels) ──► Form.ShowDefinition
  ```

  The options behave as they do on `Input.DropDown` and `Input.ListBox`, because they are the same
  options: the objects go in whole and the selected one comes back as itself. Keys are resolved
  first, so a field can be named by the key its label derives — the same one its answer arrives
  under. A default in the file naming an option that is no longer there is dropped, and the field
  opens as though the file had never named one, rather than opening blank for no visible reason.
  Naming a field that is not there, or one with nothing to replace, fails with the choice fields
  listed, because the mistake is nearly always a key spelled two ways.

- **Every node has an icon.** All 113 nodes now carry a drawn icon in Dynamo's library tree and on
  the node itself, in place of the default cube.

  They use a **family system**. The plate colour identifies the category — Input is pink, Layout
  yellow, Behavior sky, Condition lime, Compute orange, Rule violet, Result teal, Form white, and
  Theme is the one inverted plate, near-black with white line work and a pink shadow, which suits a
  family whose nodes are all about light and dark. The glyph on the plate says what the node does,
  and glyphs are shared across categories on purpose: a calendar is a calendar whether it is
  `Input.DatePicker` or `Result.GetDate`.

  That is a deliberate choice against 113 unique drawings. In the library an icon is drawn at about
  sixteen pixels — a twelve-pixel interior, which cannot separate `Condition.GreaterThan` from
  `Condition.AtLeast` however carefully it is drawn. Colour answers "which family", shape answers
  "what kind of thing", and the label beside the icon does the fine distinguishing it is already
  there to do.

  The icons match the forms: heavy outlines, flat colour, hard unblurred shadows offset down and to
  the right. They are drawn by the preview harness with the same offline WPF rendering that
  produces the documentation screenshots, and both the PNGs and the compiled resource container are
  checked in. Adding a node without an icon fails the generator *and* the test suite, because the
  symptom otherwise is a default cube nobody notices.

- **A second shipped file, `Interlude.customization.dll`**, and a change to the rule that forbade
  one.

  Dynamo reads node icons from a sibling assembly named `<AssemblyName>.customization.dll` and from
  nowhere else — there is no attribute, folder convention or manifest entry that does the same job.
  So the choice was that file or no icons.

  The rule is now **one code assembly, plus one resource assembly that is checked to be inert**.
  The new file declares no types and references nothing but the `netstandard` facade: there is
  nothing in it to bind against, nothing to conflict with another package's copy of a library, and
  nothing that can execute. The rule was never really about counting files — it was about what a
  file can collide with in the flat folder Revit shares with every add-in.

  That emptiness is verified rather than asserted, in three places: the project fails to build if it
  acquires code or a package reference, `build-all.ps1` refuses to package it if it declares a type,
  and CI re-checks types and references against what was actually packed. Nothing changes for
  existing installs; a package folder that lacks the file simply shows the old default icons.

- **Node help inside the package.** Every one of the 113 nodes now ships a Markdown help page in
  the package's `doc/` folder, so selecting a node in Dynamo and opening Help shows Interlude's own
  documentation in the panel beside the graph instead of "no documentation available". The format
  is Dynamo's own, taken from the fallback docs that ship with Dynamo Core.

  The pages are generated from the shipped assembly — signatures, port names, types and defaults by
  reflection, prose from the XML documentation file the compiler emits beside the DLL. That is the
  same file Dynamo reads for its port tooltips, so the help panel and the tooltip cannot say
  different things, and changing what a node's help says means changing the `///` comment on the
  node. A test fails when a node has no page; CI regenerates the folder and fails on any
  difference.

  `Form.Show`, `Input.TextBox` and `Input.Toggle` ship an example graph — `Interlude.Form.Show.dyn`
  and friends, in the same `doc/` folder — which Dynamo's browser offers to open, with a screenshot
  of it beneath the page. Example files are hand-placed rather than generated, deliberately:
  everything else in that folder regenerates byte-for-byte, which is what lets CI check it for
  drift, and rendering a picture on each run would differ between machines and fail every pull
  request.

  Every page also carries its family's shared rules — that choice inputs return the object rather
  than its display name, that a rule on a hidden field is never applied, that every Behavior node
  returns a new element. Those were written down but reachable only by reading the source; somebody
  who arrived at one node from the library had no way to find them.

- **The node documentation itself was substantially rewritten.** 77 of the 112 nodes had summaries
  under fifteen words — `Input.TextBox` was four — which is enough to name a node and not enough to
  use one. They now say what the answer's *type and shape* are, which sibling node to prefer and
  when, and the behaviour that surprises people: that `Input.ListBox` changes the shape of its
  answer with `allowMultiple`, that a folded `Layout.Expander` still validates and can block
  submission with an error nobody can see, that `Layout.Progress` cannot animate because the graph
  is waiting on the form, that an unanchored `Rule.Regex` passes `ABC-1234-oops`, and that
  `Input.Password` returns plain text and is held in memory with every other answer.

  This lands in Dynamo's port tooltips as well as in the help panel, since both are read from the
  same file.

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

- **Choice inputs given a list built one control per item instead of one control holding them
  all.** `Input.ListBox` fed 22 sheets produced 22 separate list boxes, each with one sheet in it
  and its own copy of the label. `Input.DropDown`, `Input.RadioButtons` and `Input.TreeSelect` did
  the same, as did `Condition.In`, `Condition.And`, `Condition.Or`, `Compute.Lookup`,
  `Behavior.WithValidation` and `Form.Options`.

  The cause was a signature mistake with no visible symptom in C#. A zero-touch parameter typed
  `object` becomes DesignScript `var`, which is rank 0 — a single value. Give a rank-0 port a list
  and Dynamo does not pass the list, it **replicates**: it calls the node once per item. Declaring
  the parameter as a collection makes it `var[]`, which takes the list whole, and costs nothing at
  the other end because DesignScript promotes a lone value into a one-item list on the way in. So
  these ports still accept a single item exactly as before.

  **This changes those node signatures**, which the append-only rule otherwise forbids. A graph
  saved against the old ones will show the node as unresolved and need it replaced. That is a real
  cost and it is worth paying once, now, before there are users: the alternative is a node that
  cannot do the thing it exists for. Nothing else about the API moved.

  `Compute.Sum` had it too, and was missed on the first pass — which is the reason the guard works
  the way it does. `ListPortTests` reads the *declarations* rather than calling the nodes, because
  none of the existing tests could see this: from C# a `List<object>` argument behaves identically
  either way, and the difference exists only inside Dynamo's evaluator. And rather than check a
  hand-written list of list-shaped ports — the approach that missed `Sum` — it walks **every** port
  typed as a bare value and fails until each has been classified as one or the other. It found
  `Layout.Image`'s two the moment it was written.

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

[Unreleased]: https://github.com/johnpierson/Interlude/compare/v1.0.3...HEAD
[1.0.3]: https://github.com/johnpierson/Interlude/releases/tag/v1.0.3
[1.0.0]: https://github.com/johnpierson/Interlude/releases/tag/v1.0.0
