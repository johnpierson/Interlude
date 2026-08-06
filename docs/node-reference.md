# Node reference

Every node Interlude ships, grouped as it appears in the Dynamo library.

Parameters are listed in **port order**, which is the order they appear on the node. Optional
parameters show their default. The machine-generated signatures live in
[`api-surface.txt`](../tests/Interlude.Tests/api-surface.txt); this page is the one with
explanations.

**Contents** — [Input](#input) · [Layout](#layout) · [Behavior](#behavior) ·
[Condition](#condition) · [Compute](#compute) · [Rule](#rule) · [Theme](#theme) ·
[Form](#form) · [Result](#result)

---

## Conventions

Three things are true of nearly every node here.

**Inputs share three trailing ports.** `key`, `tooltip` and `helpText` mean the same thing on all
seventeen of them. `key` names the answer in the results; left empty it is derived from the label.

**Choice inputs take values, not names.** `items` holds the actual objects — Revit elements,
family types, anything — and `displayNames` holds what to show for each. Selecting an option hands
back the original object, so no lookup from a string is needed. Mismatched list lengths are
tolerated: a missing display name falls back to the value's own text.

**Behavior nodes return a new element.** They never modify the one they were given, so the same
element can feed two different behaviours without one affecting the other.

---

## Input

The fields a user answers.

### Text

| Node | Parameters |
| --- | --- |
| **TextBox** | `label`, `defaultValue = ""`, `placeholder = ""`, `key`, `tooltip`, `helpText` |
| **TextArea** | `label`, `defaultValue = ""`, `lines = 4`, `placeholder = ""`, `key`, `tooltip`, `helpText` |
| **Password** | `label`, `placeholder = ""`, `key`, `tooltip`, `helpText` |

`Password` masks the field on screen; the answer comes back as plain text like any other. It
exists so nobody types a credential in front of a room — see [SECURITY.md](../SECURITY.md).

```
Input.TextBox("Prefix", defaultValue: "WIP_", placeholder: "e.g. WIP_", key: "prefix")
```

### Numbers

| Node | Parameters |
| --- | --- |
| **Number** | `label`, `defaultValue = 0`, `minimum = null`, `maximum = null`, `increment = 1`, `decimalPlaces = 2`, `unit = ""`, `key`, `tooltip`, `helpText` |
| **Integer** | `label`, `defaultValue = 0`, `minimum = null`, `maximum = null`, `increment = 1`, `unit = ""`, `key`, `tooltip`, `helpText` |
| **Slider** | `label`, `minimum = 0`, `maximum = 100`, `defaultValue = 0`, `step = 1`, `decimalPlaces = 2`, `key`, `tooltip`, `helpText` |

`minimum` and `maximum` are `null` for "no bound". They set the spinner's limits but do **not**
block typing — a half-typed number should not fight the person typing it. Use `Rule.Range` when
the bound must be enforced.

A slider does clamp, because a slider physically cannot show a value outside its track.

```
Input.Number("Offset", defaultValue: 150, minimum: 0, unit: "mm", key: "offset")
```

### Yes / no

| Node | Parameters |
| --- | --- |
| **CheckBox** | `label`, `defaultValue = false`, `key`, `tooltip`, `helpText` |
| **Toggle** | `label`, `defaultValue = false`, `onText = "On"`, `offText = "Off"`, `key`, `tooltip`, `helpText` |

A check box puts its wording beside the box rather than in the label column, which is how a check
box reads. A toggle keeps a label column and shows `onText` / `offText` beside the switch.

### Choosing

| Node | Parameters |
| --- | --- |
| **DropDown** | `label`, `items`, `displayNames`, `defaultValue`, `placeholder = ""`, `key`, `tooltip`, `helpText` |
| **RadioButtons** | `label`, `items`, `displayNames`, `defaultValue`, `horizontal = false`, `key`, `tooltip`, `helpText` |
| **ListBox** | `label`, `items`, `displayNames`, `allowMultiple = true`, `defaultValue`, `visibleRows = 6`, `key`, `tooltip`, `helpText` |
| **TreeSelect** | `label`, `nodes`, `allowMultiple = true`, `defaultValue`, `expandAll = false`, `key`, `tooltip`, `helpText` |
| **TreeItem** | `displayName`, `value`, `children`, `expanded = false`, `selectable = true` |

A dropdown selects its first option by default. Give it a `placeholder` instead and it starts with
nothing chosen, which is what you want when "not answered yet" is meaningful.

**`ListBox` and `TreeSelect` change shape with `allowMultiple`:** the answer is a *list* of chosen
objects when true, and the single chosen object when false. That saves every downstream node from
unwrapping a one-item list — and `Result.GetList` handles both if you would rather not care.

`TreeItem` nests to build a hierarchy. `selectable: false` makes a branch that only groups.

```
Input.DropDown("Wall type", items: wallTypes, displayNames: names, key: "wallType")

Input.TreeSelect("Rooms", nodes: [
    Input.TreeItem("Level 1", children: [
        Input.TreeItem("Room 101", value: room101),
        Input.TreeItem("Room 102", value: room102)])])
```

### Dates, colours, paths

| Node | Parameters |
| --- | --- |
| **DatePicker** | `label`, `defaultValue = null`, `includeTime = false`, `minimum = null`, `maximum = null`, `key`, `tooltip`, `helpText` |
| **ColorPicker** | `label`, `defaultValue = "#000000"`, `showAlpha = false`, `presets = null`, `key`, `tooltip`, `helpText` |
| **FilePath** | `label`, `defaultValue = ""`, `filter = "All files\|*.*"`, `allowMultiple = false`, `forSaving = false`, `key`, `tooltip`, `helpText` |
| **DirectoryPath** | `label`, `defaultValue = ""`, `key`, `tooltip`, `helpText` |

`DatePicker` answers `null` when left empty — a date picker that silently defaults to today has
answered a question nobody asked. Read it with `Result.GetDate`.

`ColorPicker` answers an Interlude colour. Read it with `Result.GetColor`, which breaks it into
hex plus red/green/blue/alpha numbers.

`FilePath` answers a list of paths when `allowMultiple` is true, a single path otherwise.

```
Input.FilePath("Template", filter: "Revit files|*.rvt|All files|*.*", key: "template")
Input.ColorPicker("Tint", defaultValue: "#3366CC", presets: ["#C42B1C", "#1A7F37"])
```

---

## Layout

Arranging a form, and the elements that show rather than ask.

> **Containers take lists.** There is no single-element overload, deliberately: with both
> available, passing one element to a list port makes Dynamo replicate the node and produce N
> containers of one child each instead of one container of N. Pass a list, even a list of one.

### Grouping

| Node | Parameters |
| --- | --- |
| **Section** | `header`, `elements`, `collapsible = false`, `expanded = true` |
| **Column** | `elements`, `spacing = -1` |
| **Row** | `elements`, `equalWidths = false`, `wrap = false`, `spacing = -1` |
| **Grid** | `elements`, `columns = "*, *"`, `columnSpacing = -1`, `rowSpacing = -1` |
| **Cell** | `element`, `row = 0`, `column = 0`, `rowSpan = 1`, `columnSpan = 1` |
| **Card** | `elements`, `header = ""`, `subheader = ""`, `shadow = true` |
| **Expander** | `header`, `elements`, `expanded = true` |
| **Tabs** | `pages`, `selectedIndex = 0` |
| **TabPage** | `header`, `elements` |
| **Scroll** | `elements`, `maxHeight = 300`, `allowHorizontal = false` |
| **Dock** | `elements`, `lastChildFills = true` |
| **Docked** | `element`, `side = "Left"` |
| **Split** | `first`, `second`, `horizontal = true`, `position = 0.5` |

A `spacing` of `-1` means "use the theme's spacing", which is what keeps a form consistent.

**Grid columns** use a compact syntax: `auto` sizes to content, `*` takes a share of the leftover
space, `2*` takes two shares, and a plain number is a pixel width.

```
Layout.Grid([ ... ], columns: "auto, *, 120")
```

Children fill the grid in reading order unless placed with `Layout.Cell`.

```
Layout.Section("Advanced", [
    Input.CheckBox("Verbose logging"),
    Input.TextBox("Log path")
], collapsible: true, expanded: false)
```

### Showing

| Node | Parameters |
| --- | --- |
| **Label** | `text`, `headingLevel = 0`, `muted = false` |
| **Preview** | `label`, `value`, `placeholder = ""`, `monospaced = false` |
| **Markdown** | `text` |
| **Image** | `path`, `width = null`, `height = null`, `alternateText = ""` |
| **Separator** | `caption = ""` |
| **Spacer** | `size = 8` |
| **Progress** | `value = 0`, `maximum = 100`, `indeterminate = false`, `segments = 0` |

`headingLevel` 1–4 renders as a heading; 0 is body text.

`Markdown` supports headings, **bold**, *italic*, `code`, links, bullet and numbered lists, and
horizontal rules. It is a deliberate subset — see [architecture](architecture.md).

`Preview` shows a value the form works out, live, as the fields it reads are edited. `value` is a
template — `"{prefix}{sample_name}"` — or any `Compute` node. A placeholder may carry a format
specifier: `{sequence:000}`, `{total:F2}`, `{due:yyyy-MM-dd}`.

A preview **answers nothing**: no key, never in `values`, never validated. That is what separates
it from a read-only field carrying a computed value — use that one when you need the result back
out of the form. Everything a preview shows must already be on the form, so a form renaming fifty
views previews one sample name the author puts there. See
[Preview what a form is about to do](recipes.md#preview-what-a-form-is-about-to-do).

`Progress` shows a fixed value. Nothing in the form updates it while it is open. Give it
`segments` to draw discrete cells instead of a continuous fill — "five of seven days" reads off a
segmented bar at a glance, where a bar at 71% does not.

### Buttons

| Node | Parameters |
| --- | --- |
| **Button** | `text`, `tag = ""`, `primary = false` |
| **LinkButton** | `text`, `url` |
| **ResetButton** | `text = "Reset"` |

`Button` **closes the form** and reports its `tag` as `buttonClicked`, which is how one form
offers several outcomes. `LinkButton` opens a web page and leaves the form open — restricted to
`http` and `https`. `ResetButton` puts every field back to its default.

```
Layout.Button("Place and continue", tag: "continue", primary: true)
```

---

## Behavior

Attaching behaviour to an element. Each returns a **new** element.

| Node | Parameters |
| --- | --- |
| **VisibleIf** | `element`, `condition` |
| **EnabledIf** | `element`, `condition` |
| **RequiredIf** | `element`, `condition`, `message = ""` |
| **Required** | `element`, `message = ""` |
| **WithValidation** | `element`, `rule` |
| **WithComputed** | `element`, `computation` |
| **WithKey** | `element`, `key` |
| **WithHelp** | `element`, `tooltip = ""`, `helpText = ""` |
| **WithSize** | `element`, `width`, `height`, `labelWidth`, `margin` |
| **ReadOnly** | `element`, `readOnly = true` |

**Hidden means gone.** A hidden element takes up no space, is never validated, and never blocks
submission — a required field the user cannot see would otherwise stop the form with no control to
fix it. Its value still appears in the results.

**Disabled means visible.** A disabled element is greyed out and still contributes its value.

Applying either to a **container** applies it to everything inside.

`WithValidation` accepts one rule or a list of them. `WithComputed` only accepts an input — it
raises a clear error on anything else. `WithSize`'s `labelWidth: 0` stacks the label above its
control, which suits narrow forms and long captions.

```
Behavior.VisibleIf(Input.TextArea("Reason"), Condition.IsChecked("needs_reason"))
Behavior.WithValidation(Input.TextBox("Email"), [Rule.Required(), Rule.Regex("@")])
```

---

## Condition

Tests over the form's own answers, for the Behavior nodes. Each names the field it reads by
**key**.

| Node | Parameters |
| --- | --- |
| **Equals** / **NotEquals** | `key`, `value`, `ignoreCase = false` |
| **GreaterThan** / **LessThan** | `key`, `value` |
| **AtLeast** / **AtMost** | `key`, `value` |
| **Contains** / **StartsWith** / **EndsWith** | `key`, `value`, `ignoreCase = false` |
| **IsEmpty** / **IsNotEmpty** | `key` |
| **IsChecked** / **IsNotChecked** | `key` |
| **In** | `key`, `values`, `ignoreCase = false` |
| **Matches** | `key`, `pattern`, `ignoreCase = false` |
| **And** / **Or** | `conditions` |
| **Not** | `condition` |
| **Always** | `value = true` |

Comparisons are type-aware. Numbers compare numerically even when typed as text, dates compare as
dates, lists compare element by element, and text compares case-sensitively unless `ignoreCase`
says otherwise.

`Contains` means substring for text and membership for a multi-select — which is what a graph
author means in each case.

`IsEmpty` is about *absence*: blank text or nothing selected. `false` and `0` are answers, not
emptiness.

An empty `And` is true and an empty `Or` is false, so an unwired list behaves predictably.

```
Condition.And([
    Condition.Equals("format", "DWG"),
    Condition.IsNotEmpty("folder")])
```

---

## Compute

Values worked out from other answers, for `Behavior.WithComputed`.

| Node | Parameters |
| --- | --- |
| **Field** | `key` |
| **Constant** | `value = null` |
| **Format** | `template` |
| **Sum** | `keys` |
| **Arithmetic** | `left`, `operation`, `right` |
| **Lookup** | `key`, `lookupKeys`, `lookupValues`, `fallback = null` |
| **If** | `condition`, `ifTrue`, `ifFalse` |

`Format` fills field values into a template: `"Hello {firstName} {lastName}"`. Double a brace to
write a literal one.

`Arithmetic` takes `Add`, `Subtract`, `Multiply`, `Divide`, `Modulo`, `Power`, `Min` or `Max`.
Dividing by zero gives zero rather than infinity, so a half-filled form shows a sensible total
rather than a symbol.

> **A bare string in an operand port is a field key**, not literal text — that is what it means
> nine times out of ten in that position. Unless it contains a brace, in which case it is a
> template, because a key never does: `Compute.If(c, "{prefix}{name}", "{name}")` needs no
> `Compute.Format` around either branch. Use `Compute.Constant` when you really do mean the text.

Computed fields become read-only and update whenever anything they read changes, in dependency
order. **Loops are rejected when the form is built**, before a window appears.

```
Behavior.WithComputed(Input.Number("Total"),
    Compute.Arithmetic(
        Compute.Arithmetic("quantity", "Multiply", "unitPrice"),
        "Add",
        Compute.Field("shipping")))
```

---

## Rule

Checks applied while the user types, for `Behavior.WithValidation`.

| Node | Parameters |
| --- | --- |
| **Required** | `message = ""` |
| **Range** | `minimum = null`, `maximum = null`, `message = ""` |
| **Length** | `minimum = null`, `maximum = null`, `message = ""` |
| **Regex** | `pattern`, `message = ""`, `ignoreCase = false` |
| **FileExists** | `message = ""` |
| **FolderExists** | `message = ""` |
| **CompareTo** | `otherKey`, `operation = "GreaterThan"`, `message = ""` |

**Except for `Required`, every rule passes on an empty field.** Emptiness is
`Behavior.Required`'s business, so an optional field with a range on it stays optional.

`Length` counts characters for text and items for a multi-select.

`CompareTo` reads another field, which is how "end date must be after start date" is expressed. It
re-runs when *either* field changes.

Regular expressions run with a one-second timeout so a pathological pattern cannot wedge the UI.

```
Behavior.WithValidation(Input.DatePicker("End"),
    Rule.CompareTo("start", "GreaterThan", "The end date must be after the start date."))
```

---

## Theme

| Node | Parameters |
| --- | --- |
| **Neubrutalism** | `dark = false`, `accent = ""` |
| **System** | — |
| **Light** / **Dark** | `accent = ""` |
| **Mono** | `dark = false`, `accent = ""` |
| **Create** | `mode = "Auto"`, `accent = ""`, `density = "Comfortable"`, `cornerRadius = 4`, `fontSize = 13`, `fontFamily = ""`, `labelWidth = 130`, `reducedMotion = false`, `shape = "Rounded"`, `uppercaseHeaders = false`, `headerTracking = 0`, `borderWidth = 1`, `shadowOffset = 0`, `heavyText = false` |
| **WithColors** | `theme`, `background`, `foreground`, `surface`, `border`, `error` |

**A form with nothing on its theme port is neubrutalist, in light mode.** Heavy black outlines,
square corners, solid unblurred shadows offset down and to the right, flat loud colour, and type
set hard. `Light` and `Dark` are the conventional look and are the way out of it.

The default does not follow Windows. The palette is built around cream and black, and flipping to
the inverted one because the machine happens to be set to dark is a different design rather than
the same one dimmed. `System` is the same look with `mode` set to `Auto`, for a graph that would
rather match the machine.

`mode` is `Auto`, `Light` or `Dark`; `Auto` follows the Windows app theme. `density` is `Compact`,
`Comfortable` or `Spacious`. `labelWidth: 0` stacks labels above their controls.

`shape` is `Rounded`, `Pill` or `Square`. **`Pill` ignores `cornerRadius`** and derives the radius
from the control height instead, because "fully rounded" is a function of how tall a control is.
Whatever the shape, things a few pixels across — a tick box, a progress cell — clamp their radius,
because a check box rounded into a circle looks like a radio button.

`borderWidth` and `shadowOffset` are the two knobs the neubrutalist look is built from. The shadow
is solid and unblurred, offset by that many pixels down and to the right; zero switches it off.
A card that asks for a shadow with `Layout.Card` keeps getting a *soft* one in themes where
`shadowOffset` is zero, so the two ideas do not collide.

`uppercaseHeaders` and `headerTracking` are the micro-label treatment: small spaced capitals on
section, card and tab headings, and on `Layout.Label` headings. Body text is never tracked, where
letter spacing hurts readability rather than helping it. `heavyText` sets labels, headings and
buttons in a heavier weight — thin captions beside three-pixel outlines look like a mistake.

Accent text colour is chosen automatically by contrast, so a bright accent still reads.

Themes apply to the form's own window and nothing else — Revit's UI is never touched.

```
Theme.Neubrutalism()            // the default, explicitly
Theme.Neubrutalism(dark: true, accent: "#00E5FF")
Theme.Light()                   // the conventional look
Theme.Mono()                    // black, white, pills, spaced capitals
Theme.Dark("#4C8DFF")
Theme.Create(borderWidth: 3, shadowOffset: 6, shape: "Square", heavyText: true)
Theme.Create(shape: "Pill", uppercaseHeaders: true, headerTracking: 0.08, labelWidth: 0)
```

### The font

Interlude sets its own font by default: **Space Grotesk**, embedded inside `Interlude.dll` so it
renders identically on every machine rather than depending on what happens to be installed. Name
any other font on `Create`'s `fontFamily` port to override it; Interlude falls back to Segoe UI
Variable Text, Segoe UI and Tahoma if a named font is missing.

Four static faces are embedded — Light, Regular, Medium and Bold — rather than the variable font
Google Fonts distributes. WPF cannot use variable font axes, so a variable file would give one
weight and a synthetic bold for everything `heavyText` sets in heavy type.

It is SIL Open Font Licensed — see [THIRD-PARTY-NOTICES.md](../THIRD-PARTY-NOTICES.md).

---

## Form

| Node | Returns |
| --- | --- |
| **Show** | `values`, `wasSubmitted`, `buttonClicked`, `form` |
| **ShowDefinition** | `values`, `wasSubmitted`, `buttonClicked`, `form` |
| **Create** | `FormDefinition` |
| **Options** | `FormOptions` |
| **Check** | `isValid`, `messages` |
| **ToJson** / **FromJson** | `string` / `FormDefinition` |
| **WithOptions** | `FormDefinition` |
| **Forget** | `bool` |

### Show

```
Form.Show(title, elements, trigger = true, submitText = "Submit", cancelText = "Cancel",
          width = 420, maxHeight = 800, formId = "", rememberValues = true,
          headlessUseDefaults = false, theme = null, options = null)
```

| Port | What it does |
| --- | --- |
| `trigger` | **Exactly `false`** skips the dialog and returns the last answers. Anything else shows it. Doubles as a sequencing input. |
| `formId` | Identifies the form across runs, for remembered answers. Derived from the title and field keys when empty. |
| `rememberValues` | Pre-fills with the answers the form was last **submitted** with. Cancelling never overwrites them. |
| `headlessUseDefaults` | With no UI available: `false` stops the graph with an explanation, `true` returns every field's default. |
| `options` | `Form.Options` — description, height, resizable, cancel button, extra footer buttons, icon. |

`Show`'s signature is append-only for ever, because saved graphs bind to parameter *positions*.
New settings arrive through `options` rather than as new ports.

### The others

`Create` builds a form without showing it — for saving to JSON, or for showing later with
`ShowDefinition`.

`Check` reports authoring problems without showing anything: conditions naming fields that do not
exist, duplicate keys, loops between computed values. Worth wiring while building a complex form.

`WithOptions` replaces the options of one drop-down, radio group or list box in a form that already
exists — the way a form loaded from a file gets its Revit elements, which no file can carry:

```
Form.WithOptions(form, key, items = null, displayNames = null)
```

It resolves keys before looking, so a field can be named by the key its label derives. It returns a
new form and changes nothing in place. Naming a field that does not exist, or one with no options
to replace, is an error that names the fields it could have filled in.

`Forget` clears remembered answers for one form, or for all of them when given an empty id.

---

## Result

Reading answers. **Every node here accepts either the `values` dictionary or the `form` output**,
so it does not matter which is to hand.

| Node | Returns |
| --- | --- |
| **GetString** | `string`, `fallback = ""` |
| **GetNumber** | `double`, `fallback = 0` |
| **GetInteger** | `int`, `fallback = 0` |
| **GetBool** | `bool`, `fallback = false` |
| **GetDate** | `DateTime?`, `fallback = null` |
| **GetColor** | `hex`, `red`, `green`, `blue`, `alpha` |
| **GetList** | `List<object>` |
| **GetFilePaths** | `List<string>` |
| **ValueByKey** | the raw answer, `fallback = null` |
| **Keys** / **Values** | every field name / every answer, both sorted the same way |
| **HasKey** | `bool` |
| **WasSubmitted** / **WasCancelled** | `bool` |
| **ButtonClicked** | `"submit"`, `"cancel"`, `"closed"`, `"skipped"`, or a custom tag |

Each accessor takes a **fallback** used when the field is missing or empty, which is what keeps a
downstream node from receiving a null it was not expecting.

`GetList` wraps a single answer in a one-item list, so a downstream node need not care whether the
field allowed several.

`WasSubmitted` and `ButtonClicked` need the `form` output — the `values` dictionary alone does not
carry the outcome.

> **Check `wasSubmitted` before acting.** A cancelled form returns a complete, valid-looking set
> of defaults. That is the point — but it means acting without checking will cheerfully do the
> work the user just backed out of.

```
Result.GetString(values, "prefix", fallback: "WIP_")
Result.GetColor(form, "tint")   →  hex, red, green, blue, alpha
Result.ButtonClicked(form)      →  "continue"
```

---

## See also

- [Recipes](recipes.md) — worked patterns for real forms
- [Forms as JSON](form-json.md) — the schema, and what does and does not survive a round trip
- [Coming from Data-Shapes](migrating-from-data-shapes.md)
- [Architecture](architecture.md) — how it works, and why
