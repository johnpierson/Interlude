# Recipes

Patterns that come up in real forms. Each one is short, complete, and explains the reasoning
rather than just the wiring.

**Contents** —
[Gate a form behind a button](#gate-a-form-behind-a-button) ·
[One form, several outcomes](#one-form-several-outcomes) ·
[Dependent dropdowns](#dependent-dropdowns) ·
[A wizard](#a-wizard) ·
[Progressive disclosure](#progressive-disclosure) ·
[Cross-field validation](#cross-field-validation) ·
[A live total](#a-live-total) ·
[Remember answers between runs](#remember-answers-between-runs) ·
[Run a graph unattended](#run-a-graph-unattended) ·
[Ship a form as a file](#ship-a-form-as-a-file) ·
[Pick Revit elements](#pick-revit-elements) ·
[Match Revit's theme](#match-revits-theme) ·
[Long forms](#long-forms) ·
[Debug a form](#debug-a-form)

---

## Gate a form behind a button

**The problem.** In Automatic run mode the dialog reappears every time anything upstream changes.
Even in Manual mode, re-running a graph to fix something downstream re-asks a question that was
already answered.

**The fix.** Wire a boolean into `trigger`. Exactly `false` skips the dialog and returns the last
answers.

```
Boolean node ──► Form.Show(trigger: ...)
```

Flip it to `true`, run, flip it back to `false`. Every subsequent run reuses the answers.

`trigger` doubles as a sequencing input: wire the *output* of some earlier node into it when the
form must not appear until that work is done.

> Manual run mode is still the right setting for any graph built around a form. `trigger` makes it
> pleasant; it does not make Automatic mode a good idea.

---

## One form, several outcomes

**The problem.** "Place", "Place and continue", and "Preview" are three answers to one question,
and three separate forms is three times the maintenance.

**The fix.** `Layout.Button` closes the form and reports its tag.

```
Form.Show("Place families", [
    Input.DropDown("Family type", items: types, key: "type"),
    Input.Integer("Count", defaultValue: 1, key: "count"),
    Layout.Row([
        Layout.Button("Preview", tag: "preview"),
        Layout.Button("Place", tag: "place", primary: true)
    ])
])
```

Then branch on `Result.ButtonClicked(form)` — `"preview"`, `"place"`, or `"cancel"` if they backed
out.

Footer buttons work too, via `Form.Options(extraButtons: [...])`, if you would rather they sat
beside Submit than in the body.

---

## Dependent dropdowns

**The problem.** Choosing a category should narrow the family list.

**The honest answer.** Interlude cannot repopulate a dropdown's *items* while the form is open —
options are fixed when the form is built. A form is a value, and the value does not change while
it is on screen.

**What to do instead**, in order of how well it works:

**Show one dropdown per category and hide the rest.** Fine for a handful of categories, and the
answers stay separate:

```
Behavior.VisibleIf(
    Input.DropDown("Wall type", items: wallTypes, key: "wallType"),
    Condition.Equals("category", "Walls"))

Behavior.VisibleIf(
    Input.DropDown("Door type", items: doorTypes, key: "doorType"),
    Condition.Equals("category", "Doors"))
```

Only the visible one is validated, and both appear in the results — read whichever matches the
chosen category.

**Or use two forms.** Ask for the category, then build the second form from the answer:

```
Form.Show("Category", ...) ──► filter families ──► Form.Show("Family", items: filtered)
```

Two dialogs is a real cost, but it scales to any number of categories and the second form is built
from live data.

**Or show everything with a filter.** `Input.ListBox` has a search box; a single list of every
family, filtered by typing, is often better than two clicks through a hierarchy.

---

## A wizard

**The problem.** Twenty fields is intimidating in one column.

**The fix.** Tabs. Everything is answered in one dialog, so there is no back-and-forth state to
manage, but the user sees one page at a time.

```
Form.Show("Export", [
    Layout.Tabs([
        Layout.TabPage("What", [
            Input.ListBox("Views", items: views, key: "views")
        ]),
        Layout.TabPage("Where", [
            Input.DirectoryPath("Folder", key: "folder"),
            Input.TextBox("Prefix", key: "prefix")
        ]),
        Layout.TabPage("How", [
            Input.DropDown("Format", items: ["DWG", "IFC", "PDF"], key: "format"),
            Input.CheckBox("Overwrite existing", key: "overwrite")
        ])
    ])
], width: 620)
```

A field failing validation on a hidden tab still blocks submission, and the error is on a page the
user cannot see. Keep required fields on the first tab where you can, and lean on `helpText`.

---

## Progressive disclosure

**The problem.** Advanced options that nine users in ten never need.

**The fix.** A collapsible section, closed by default.

```
Layout.Section("Advanced", [
    Input.Number("Tolerance", defaultValue: 0.001, key: "tolerance"),
    Input.CheckBox("Verbose logging", key: "verbose")
], collapsible: true, expanded: false)
```

Unlike `VisibleIf`, a collapsed section's fields are still *visible* to the form — they are
validated and they can be required. That is the right behaviour here: the user can open the
section and fix the problem.

Use `Behavior.VisibleIf` on the section instead when the options genuinely do not apply.

---

## Cross-field validation

**The problem.** "End must be after start" involves two fields, and it has to re-check when
*either* changes.

**The fix.** `Rule.CompareTo` reads another field by key and declares the dependency, so the
session re-runs it when that field moves.

```
Behavior.WithValidation(
    Input.DatePicker("End", key: "end"),
    Rule.CompareTo("start", "GreaterThan", "The end date must be after the start date."))
```

Works for numbers too — a maximum that must exceed a minimum.

---

## A live total

**The problem.** A quantity and a unit price should show a total that keeps up.

**The fix.** A computed field. It becomes read-only and recalculates in dependency order.

```
Behavior.WithComputed(
    Input.Number("Subtotal", unit: "£", key: "subtotal"),
    Compute.Arithmetic("quantity", "Multiply", "unitPrice"))

Behavior.WithComputed(
    Input.Number("Total", unit: "£", key: "total"),
    Compute.Arithmetic("subtotal", "Add", "vat"))
```

Chains are fine and settle in one pass — `total` sees the *new* `subtotal`, not last run's. Declare
them in any order; the dependency graph decides.

A loop (`a` from `b`, `b` from `a`) is rejected when the form is built, with both keys named.

For a text summary, `Compute.Format`:

```
Behavior.WithComputed(
    Input.TextBox("Summary", key: "summary"),
    Compute.Format("{quantity} items for {orderedBy}, £{total} including VAT"))
```

---

## Remember answers between runs

On by default. A form re-opens with the answers it was last **submitted** with; cancelling never
overwrites them.

Give the form a stable `formId` if you want that memory to survive edits to the form:

```
Form.Show("Export", [...], formId: "acme.export-views")
```

Without one, the id is derived from the title and field keys — so adding a field starts a fresh
memory rather than half-restoring the old one. That is usually what you want; a `formId` is for
when it is not.

`Form.Forget("acme.export-views")` clears it. `Form.Forget()` clears everything.

Memory lives in the Dynamo process and is gone when Dynamo closes. To persist across sessions,
write the answers yourself with `Form.ToJson`-style serialisation and feed them back as defaults.

---

## Run a graph unattended

**The problem.** A graph with a form is scheduled, or run from the command line, or through
Generative Design. There is nobody to answer it.

**The fix.** Decide explicitly which you want:

```
headlessUseDefaults: false   (default)  → the graph stops, with an explanation
headlessUseDefaults: true              → every field's default, wasSubmitted = false
```

The default is to stop, deliberately. A graph that silently proceeds with defaults having asked
nobody anything is a graph that quietly does the wrong thing at 3am.

When you do opt in, check `wasSubmitted` and treat `false` as "nobody chose this" — because nobody
did.

---

## Ship a form as a file

**The problem.** The same form is needed in five graphs, and five copies drift apart.

**The fix.** Build it once, save it, load it.

```
Form.Create("Export", [...]) ──► Form.ToJson ──► write to disk
```

```
read from disk ──► Form.FromJson ──► Form.ShowDefinition(trigger)
```

The file can live in a repository, be reviewed in a pull request, and be edited by hand — the
[preview harness](../tools/Interlude.Preview) reloads and reshows it whenever it is saved.

Use `Result.HasKey` in a graph reading a form it did not build. See [form-json.md](form-json.md)
for what does and does not survive the round trip — live Revit elements as dropdown options being
the notable exception.

---

## Pick Revit elements

Interlude has no Revit dependency, so there are no Revit-aware picker nodes. There do not need to
be: collect elements in the graph and pass them as `items`.

```
All Elements of Category ──► items ─┐
Element.Name             ──► displayNames ─┴─► Input.DropDown("Wall type", key: "wallType")
```

`Result.ValueByKey(values, "wallType")` gives back the **element**, not its name.

For several:

```
Input.ListBox("Rooms", items: rooms, displayNames: roomNames, allowMultiple: true, key: "rooms")
Result.GetList(values, "rooms")   →  the room elements
```

A form built this way cannot be saved to JSON and reopened in another model — the elements do not
exist there. Build the options in the graph each run.

---

## Match Revit's theme

The default look is neubrutalist — heavy outlines, hard shadows, loud flat colour — and it follows
the Windows light/dark setting, which is usually what Revit follows too. It is a deliberate choice,
and it is not the right one for a dialog that has to disappear into a corporate deployment.

For the conventional look, wire a theme:

```
Theme.Light()
Theme.Dark("#4C8DFF")
Theme.Create(mode: "Dark", accent: "#F0A500", density: "Compact", cornerRadius: 2)
```

To keep the default and only turn it down a little, start from it:

```
Theme.Neubrutalism(dark: true, accent: "#4C8DFF")
Theme.Create(borderWidth: 1, shadowOffset: 2, shape: "Square", heavyText: true)
```

Accent text colour is picked by contrast, so a bright accent still reads.

Interlude never writes to the host application's resources, so nothing you do here can leak into
Revit's own UI.

---

## Long forms

- `Layout.Tabs` first — see [a wizard](#a-wizard).
- `density: "Compact"` and `labelWidth: 0` fit noticeably more on screen.
- `maxHeight` sets where the form starts scrolling; `width` is worth raising past 420 for anything
  with side-by-side fields.
- `Layout.Row` puts related short fields on one line:

```
Layout.Row([
    Input.Number("Width", unit: "mm", key: "width"),
    Input.Number("Height", unit: "mm", key: "height")
])
```

- `Layout.Grid` with `columns: "auto, *"` aligns a column of labels against a column of controls
  more tightly than the default label column does.

---

## Debug a form

**`Form.Check`** reports what Interlude can see wrong without showing anything: conditions naming
fields that do not exist, duplicate keys, loops between computed values.

```
Form.Create(...) ──► Form.Check ──► isValid, messages
```

The commonest finding is a typo in a condition's key. A condition on a key no field uses always
reads as empty, so the field it controls never appears — and nothing else tells you why.

**`Result.Keys`** lists what a form actually answered, which settles any question about what a
label slugified to.

**`Form.ToJson`** is the fastest way to ask for help: it is the whole form, reproducible by
anyone.

**The preview harness** renders any form JSON in either theme, with hot reload:

```bash
dotnet run --project tools/Interlude.Preview
```

---

## See also

- [Node reference](node-reference.md) — every node and every port
- [Forms as JSON](form-json.md)
- [Coming from Data-Shapes](migrating-from-data-shapes.md)
