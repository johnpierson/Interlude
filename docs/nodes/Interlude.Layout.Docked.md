## In Depth

`Layout.Docked(element, side: "Left")`

Attaches an element to one edge of a `Layout.Dock`.

Order decides the corners. Each docked element takes its whole edge out of what remains, so a Top followed by a Left gives a banner across the full width with the sidebar beneath it, while a Left followed by a Top gives a full-height sidebar with the banner beside it.

The inputs are:

- `element` (_FormElement_) — The element to place.
- `side` (_string_, defaults to `"Left"`) — One of Left, Top, Right or Bottom.

Returns `element` — The placed element.

Search terms: `dock`, `side`, `edge`, `left`, `right`, `top`, `bottom`.

___
## About the Layout nodes

Arranging and decorating a form: sections, rows, grids, tabs, and the elements that show something rather than ask something.

Every container takes a list of elements. None of them has a single-element overload, on purpose: with both available, a graph that passes one element to a list port gets replication instead of a container, and produces N containers of one child each rather than one container of N. Pass a list, even a list of one.
