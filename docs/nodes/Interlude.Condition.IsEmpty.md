## In Depth

`Condition.IsEmpty(key)`

True when the field has no answer: blank text, or nothing selected. Note that false and zero are answers, not emptiness.

The inputs are:

- `key` (_string_) — The field to read.

Returns `condition` — The condition.

Search terms: `empty`, `blank`, `unanswered`, `null`, `nothing`.

___
## About the Condition nodes

Tests over a form's own answers, for use with the Behavior nodes.

Conditions name the field they read by its key — the same key the answer appears under in the results. They are re-evaluated whenever that field changes, so a form's behaviour is described once, declaratively, rather than wired up event by event.

Comparisons are type-aware: numbers compare numerically even when typed as text, lists compare element by element, and text comparison is case-sensitive unless `ignoreCase` says otherwise.

___
## Example File

An example graph ships beside this page as `Interlude.Condition.IsEmpty.dyn`.

![Condition.IsEmpty](https://raw.githubusercontent.com/johnpierson/Interlude/main/docs/nodes/Interlude.Condition.IsEmpty_img.png)

The form it builds:

![Condition.IsEmpty form](./Interlude.Condition.IsEmpty_form.png)
