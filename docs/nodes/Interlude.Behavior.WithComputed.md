## In Depth

`Behavior.WithComputed(element, computation)`

Drives the element's value from other fields instead of from the user. The field becomes read-only and updates whenever anything it depends on changes.

The inputs are:

- `element` (_FormElement_) — The element to drive.
- `computation` (_ComputedValue_) — Built with the Compute nodes.

Returns `element` — A copy of the element with the computation attached.

Search terms: `computed`, `calculated`, `derived`, `formula`, `expression`.
