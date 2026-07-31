# Coming from Data-Shapes

Data-Shapes established the pattern of building a form from Dynamo nodes, and a great deal of
good work runs on it. Interlude is a different take on the same problem, and this page maps one
onto the other.

**The two are independent packages and can be installed side by side.** Interlude uses its own
namespaces and no `UI.*` class names, so nothing collides. There is no need to migrate anything
that already works.

---

## Read this part first

### Cancelling returns defaults, not nulls

This is the difference most likely to bite, and the one worth being deliberate about.

| | Data-Shapes | Interlude |
| --- | --- | --- |
| User cancels | The result is `null` | Every field's default value, with `wasSubmitted: false` |

```
// Data-Shapes: guard everywhere, or fail three nodes later
if (result == null) { ... }

// Interlude: check the flag, then read normally
Result.WasSubmitted(form)  →  false
values["prefix"]           →  "WIP_"   (the default, never null)
```

Returning null on cancel pushes a null check into every downstream node, and when one is missed
the failure surfaces somewhere unrelated to cancelling. Interlude always returns a complete set
of answers and reports the outcome separately.

**What this means for you:** check `wasSubmitted` before acting on the answers. If you act
without checking, a cancelled form will quietly do the work with default values — which is
exactly the mistake the null was trying to prevent. Wire `wasSubmitted` into a `ScopeIf` or an
`If` node.

### Results are keyed, not positional

| | Data-Shapes | Interlude |
| --- | --- | --- |
| Reading answers | `result[0]`, `result[1]` — positional | `values["prefix"]` — by name |

Positional results mean inserting a field in the middle of a form silently shifts every index
after it. Named results do not. Keys come from the label (`"Wall Type"` → `wall_type`) or from an
explicit `key` on the input.

**Give explicit keys to anything you intend to keep**, otherwise renaming a label renames the
answer.

---

## Node mapping

### Inputs

| Data-Shapes | Interlude |
| --- | --- |
| `UI.MultipleInputForm++` | `Form.Show` |
| `Data-Shapes.UI.SingleInputForm` | `Form.Show` with one element |
| `StringInputData` | `Input.TextBox` |
| `StringInputData` (multiline) | `Input.TextArea` |
| `NumberInputData` | `Input.Number` |
| `SliderData` | `Input.Slider` |
| `IntegerSliderData` | `Input.Integer` |
| `BooleanInputData` | `Input.CheckBox` or `Input.Toggle` |
| `SelectionData` | `Input.DropDown` |
| `MultipleInputData` | `Input.ListBox` |
| `DateInputData` | `Input.DatePicker` |
| `ColorInputData` | `Input.ColorPicker` |
| `FilePathData` | `Input.FilePath` |
| `DirectoryPathData` | `Input.DirectoryPath` |
| `ImageData` | `Layout.Image` |
| `TextData` | `Layout.Label` or `Layout.Markdown` |
| `SeparatorData` | `Layout.Separator` |
| — | `Input.Password`, `Input.RadioButtons`, `Input.TreeSelect` |

### Reading results

| Data-Shapes | Interlude |
| --- | --- |
| `result[0]` | `Result.GetString(values, "key")` |
| Manual casting | `Result.GetNumber`, `GetBool`, `GetDate`, `GetColor`, `GetFilePaths`, `GetList` |
| — | `Result.WasSubmitted`, `Result.WasCancelled`, `Result.ButtonClicked` |

### Selections give back objects

Both packages preserve this, and it is worth restating because it is what dropdowns are for:

```
Input.DropDown("Wall type", items: wallTypes, displayNames: names)
```

The answer is the wall type *element*, not its name. No lookup back from a string.

---

## What is new

Things with no Data-Shapes equivalent, roughly in order of how much they change a form.

### Behaviour is described, not wired

```
Behavior.VisibleIf(element, Condition.IsChecked("advanced"))
Behavior.EnabledIf(element, Condition.Equals("mode", "custom"))
Behavior.RequiredIf(element, Condition.IsNotEmpty("other"))
```

Fields appear, enable and become required in response to other fields. A hidden field is never
required and never blocks submission.

### Computed values

```
Behavior.WithComputed(Input.Number("Total"),
    Compute.Arithmetic("quantity", "Multiply", "unit_price"))
```

Recalculated in dependency order as their inputs change. Loops are rejected when the form is
built, not discovered as a hang.

### Live validation

```
Behavior.WithValidation(Input.TextBox("Code"), Rule.Regex("^[A-Z]{3}-[0-9]{4}$"))
Behavior.Required(Input.TextBox("Name"))
```

Rules run as the user types and block submission while any fails.

### Real layout

Data-Shapes gives you one vertical stack. Interlude has `Layout.Row`, `Grid`, `Tabs`, `Card`,
`Section`, `Expander`, `Split`, `Dock`, `Scroll`.

### Theming

`Theme.Dark`, `Theme.Light`, `Theme.Create` — accent colour, density, corner radius, fonts.
Scoped to the form's own window; Revit's UI is never touched.

### Forms are documents

`Form.ToJson` and `Form.FromJson`. A form can be checked into a repository, reviewed in a pull
request and loaded by a graph that did not build it.

### Explicit re-execution control

`trigger: false` skips the dialog and returns the last answers. A form already on screen is never
opened twice.

### Named outcomes

`Layout.Button("Place and continue", tag: "continue")` closes the form and reports its tag as
`buttonClicked`, so one form can offer several outcomes.

---

## A worked example

Data-Shapes, roughly:

```
StringInputData("Prefix", "WIP_")           ─┐
BooleanInputData("Include sheets", false)   ─┴─► MultipleInputForm++("Rename views")
                                                          │
                                                          ├─► result[0]  →  prefix
                                                          └─► result[1]  →  include sheets
                                            (and a null check on result)
```

Interlude:

```
Input.TextBox("Prefix", "WIP_", key: "prefix")            ─┐
Input.CheckBox("Include sheets", key: "includeSheets")    ─┴─► Form.Show("Rename views")
                                                                    │
                                                                    ├─ values
                                                                    │    ├─ Result.GetString(values, "prefix")
                                                                    │    └─ Result.GetBool(values, "includeSheets")
                                                                    └─ wasSubmitted  ──► gate the work
```

---

## Things to watch

- **Check `wasSubmitted`.** Repeated because it is the one that silently does the wrong thing.
- **Give explicit keys.** Derived keys follow the label; renaming a label renames the answer.
- **Manual run mode.** True of both packages. A form in Automatic mode reopens whenever anything
  upstream changes. Interlude's latch stops the dialogs stacking, and `trigger` lets you gate it,
  but Manual is still the right setting.
- **Containers take lists.** `Layout.Row` takes a list of elements. Passing a single element to a
  list port makes Dynamo replicate the node and produce N one-child rows instead of one row.
- **No Revit-aware pickers yet.** Data-Shapes has some Revit-specific selection nodes. Interlude
  has no Revit dependency at all, so those live outside it. `Input.DropDown` with elements as
  items covers most of the ground.

## Running both

Install both. They share no namespaces, no class names and no assemblies. Migrate a graph when
there is a reason to — a form that needs conditional fields, validation or a layout — and leave
the rest alone.

## See also

- [Node reference](node-reference.md) — every node and every port
- [Recipes](recipes.md) — worked patterns, including several that have no Data-Shapes equivalent
