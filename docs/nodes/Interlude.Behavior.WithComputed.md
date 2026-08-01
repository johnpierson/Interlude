## In Depth

`Behavior.WithComputed(element, computation)`

Drives the element's value from other fields instead of from the user. The field becomes read-only and updates whenever anything it depends on changes.

The inputs are:

- `element` (_FormElement_) — The element to drive.
- `computation` (_ComputedValue_) — Built with the Compute nodes.

Returns `element` — A copy of the element with the computation attached.

Search terms: `computed`, `calculated`, `derived`, `formula`, `expression`.

___
## About the Behavior nodes

Adds behaviour to an element: when it is visible, when it is enabled, when it is required, what makes it valid, and what its value is computed from.

Every node here returns a new element rather than changing the one it was given. Elements are values, so the same element can be fed into two different behaviours without one of them affecting the other, and re-running a graph rebuilds the tree from scratch with nothing left over from last time.
