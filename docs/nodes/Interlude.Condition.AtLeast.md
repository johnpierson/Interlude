## In Depth

`Condition.AtLeast(key, value)`

True when the field's answer is greater than or equal to the value.

The inputs are:

- `key` (_string_) — The field to read.
- `value` (_object_) — What to compare against.

Returns `condition` — The condition.

Search terms: `at least`, `minimum`, `greater or equal`, `>=`.

___
## About the Condition nodes

Tests over a form's own answers, for use with the Behavior nodes.

Conditions name the field they read by its key — the same key the answer appears under in the results. They are re-evaluated whenever that field changes, so a form's behaviour is described once, declaratively, rather than wired up event by event.

Comparisons are type-aware: numbers compare numerically even when typed as text, lists compare element by element, and text comparison is case-sensitive unless `ignoreCase` says otherwise.

___
## Example File

An example graph ships beside this page as `Interlude.Condition.AtLeast.dyn`.

![Condition.AtLeast](https://raw.githubusercontent.com/johnpierson/Interlude/main/docs/nodes/Interlude.Condition.AtLeast_img.png)

The form it builds:

![Condition.AtLeast form](./Interlude.Condition.AtLeast_form.png)
