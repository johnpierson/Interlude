## In Depth

`Behavior.WithHelp(element, tooltip: "", helpText: "")`

Adds hover text and a line of guidance under the element.

The two are for different readers. `tooltip` is found only by someone who already suspects there is more to know; `helpText` sits under the field where everyone reads it. Put the thing users get wrong in the help text, and the detail in the tooltip.

A line of help under the field it belongs to beats a paragraph of `Layout.Label` above the group, because the reader never has to work out which field it was about.

The inputs are:

- `element` (_FormElement_) — The element to annotate.
- `tooltip` (_string_, defaults to `""`) — Hover text.
- `helpText` (_string_, defaults to `""`) — A line of guidance shown under the element.

Returns `element` — A copy of the element with the text attached.

Search terms: `tooltip`, `help`, `hint`, `description`, `guidance`.

___
## About the Behavior nodes

Adds behaviour to an element: when it is visible, when it is enabled, when it is required, what makes it valid, and what its value is computed from.

Every node here returns a new element rather than changing the one it was given. Elements are values, so the same element can be fed into two different behaviours without one of them affecting the other, and re-running a graph rebuilds the tree from scratch with nothing left over from last time.
