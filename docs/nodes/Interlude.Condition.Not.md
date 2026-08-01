## In Depth

`Condition.Not(condition)`

Inverts a condition: true where it was false, and false where it was true.

Often avoidable — there is already `Condition.NotEquals`, `Condition.IsEmpty` and `Condition.IsNotChecked` — and the direct one reads better in a graph than a negated one. Where this earns its place is inverting something composite: not (A and B).

The inputs are:

- `condition` (_ConditionExpr_) — The condition to invert.

Returns `condition` — The condition.

Search terms: `not`, `invert`, `negate`, `opposite`.

___
## About the Condition nodes

Tests over a form's own answers, for use with the Behavior nodes.

Conditions name the field they read by its key — the same key the answer appears under in the results. They are re-evaluated whenever that field changes, so a form's behaviour is described once, declaratively, rather than wired up event by event.

Comparisons are type-aware: numbers compare numerically even when typed as text, lists compare element by element, and text comparison is case-sensitive unless `ignoreCase` says otherwise.
