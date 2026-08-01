## In Depth

`Condition.In(key, values, ignoreCase: false)`

True when the field's answer is one of the listed values.

The inputs are:

- `key` (_string_) — The field to read.
- `values` (_object_) — The values to accept.
- `ignoreCase` (_boolean_, defaults to `false`) — Ignore letter case when comparing text.

Returns `condition` — The condition.

Search terms: `in`, `one of`, `among`, `any of`.

___
## About the Condition nodes

Tests over a form's own answers, for use with the Behavior nodes.

Conditions name the field they read by its key — the same key the answer appears under in the results. They are re-evaluated whenever that field changes, so a form's behaviour is described once, declaratively, rather than wired up event by event.

Comparisons are type-aware: numbers compare numerically even when typed as text, lists compare element by element, and text comparison is case-sensitive unless `ignoreCase` says otherwise.
