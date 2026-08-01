## In Depth

`Compute.Format(template)`

Fills field values into a template: `"Hello {firstName} {lastName}"`. Write a literal brace by doubling it.

The inputs are:

- `template` (_string_) — The text, with field keys in braces.

Returns `computation` — The computation.

Search terms: `format`, `template`, `interpolate`, `text`, `concat`, `string`.

___
## About the Compute nodes

Values worked out from other answers, for use with `Behavior.WithComputed`.

A computed field is driven by the form rather than by the user: it recalculates whenever anything it reads changes, in dependency order, so a total built on a subtotal is always consistent. Computed values that depend on each other in a loop are rejected when the form is built, before a window appears.
