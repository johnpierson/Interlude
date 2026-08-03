# A first form

**Saved from Dynamo 4.2.** A text box and a toggle, shown as a form, with the answers watched.
It is the graph Dynamo offers from the help panel for [`Form.Show`](../nodes/Interlude.Form.Show.md),
[`Input.TextBox`](../nodes/Interlude.Input.TextBox.md) and
[`Input.Toggle`](../nodes/Interlude.Input.Toggle.md), and the smallest thing worth opening first.

[Download Interlude.Form.Show.dyn](../nodes/Interlude.Form.Show.dyn){ .dyn-download download }

## What is in it

```
"Your Name" ──────────────┐
"Enter your name here" ───┴─► Input.TextBox ─────┐
                                                 │
"Enable Feature" ─┐                              ├─► List.Create ─┐
false ────────────┤                              │                │
"On" ─────────────┼─► Input.Toggle ──────────────┘                │
"Off" ────────────┘                                               │
                                                                  │
"My Simple Form" ─────────────────────────────────────────────────┴─► Form.Show ─► Watch
```

Every argument is a separate node rather than a typed-in default, which makes the graph longer
than it needs to be and much easier to take apart: change the string, re-run, see what moved.

Two things to notice, because they are the two that surprise people:

**`elements` is a list.** `List.Create` gathers the text box and the toggle into the one port.
Adding a field means adding a wire to that node — not a second `Form.Show`.

**The graph runs automatically.** It is saved in Automatic run mode, so the form appears as soon
as it opens. That is the right setting for a two-minute demonstration and the wrong one for
anything real: Dynamo re-runs a graph whenever anything upstream changes, and a node that shows a
dialog will show it again. Switch to Manual, or gate the form with the `trigger` port. The
reasoning is in [Recipes](../recipes.md) and in the note on re-execution in
[`Form.Show`](../nodes/Interlude.Form.Show.md).

## Reading the answers

The `Watch` node hangs off `values`, which is a dictionary keyed by a slug of each label —
`your_name` and `enable_feature` here. Reading one by name is a Result node:

```
Result.GetString(values, "your_name")     →  "Ada"
Result.GetBool(values, "enable_feature")  →  true
```

Give real keys to anything you intend to keep. Left empty, the key is derived from the label, so
renaming the label renames the answer and quietly breaks whatever downstream node read it.

## Before you act on it

Cancelling a form returns every field's **default**, not nulls, with `wasSubmitted` false. A
graph that reads `values` without checking `wasSubmitted` first will do the work anyway, with the
defaults, and look like it succeeded:

```
Result.WasSubmitted(form)  →  false, when the user cancelled
```

That is the single behaviour most worth internalising before building anything on top of this.
[Recipes](../recipes.md) opens with the pattern for it.

## Where to go next

- [Recipes](../recipes.md) — gating a form, several outcomes, wizards, live totals.
- [Node reference](../node-reference.md) — every node and every port in one page.
- [Sample graphs](index.md) — how to add one of your own.
