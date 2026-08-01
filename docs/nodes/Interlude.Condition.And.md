## In Depth

`Condition.And(conditions)`

True when every one of the given conditions is true. An empty list is true.

The inputs are:

- `conditions` (_list of object_) — The conditions to combine.

Returns `condition` — The condition.

Search terms: `and`, `all`, `every`, `both`.

___
## About the Condition nodes

Tests over a form's own answers, for use with the Behavior nodes.

Conditions name the field they read by its key — the same key the answer appears under in the results. They are re-evaluated whenever that field changes, so a form's behaviour is described once, declaratively, rather than wired up event by event.

Comparisons are type-aware: numbers compare numerically even when typed as text, lists compare element by element, and text comparison is case-sensitive unless `ignoreCase` says otherwise.
