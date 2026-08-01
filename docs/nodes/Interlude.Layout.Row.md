## In Depth

`Layout.Row(elements, equalWidths: false, wrap: false, spacing: -1)`

Elements laid out left to right, each taking the width it asks for.

The right shape for things that belong together on one line — a width beside a height, a path beside its Browse button. Each element keeps its own label, so a row of three inputs reads as three fields rather than one.

A row does not wrap. When the contents outgrow the form's width they are squeezed rather than moved to a second line, so use `Layout.Grid` when the columns need to be told how to share the space.

The inputs are:

- `elements` (_list of FormElement_) — What goes inside.
- `equalWidths` (_boolean_, defaults to `false`) — Give every element the same width.
- `wrap` (_boolean_, defaults to `false`) — Move onto a new line when the row runs out of width.
- `spacing` (_number_, defaults to `-1`) — Gap between elements. Negative uses the theme's spacing.

Returns `element` — The form element.

Search terms: `row`, `horizontal`, `hstack`, `side by side`.

___
## About the Layout nodes

Arranging and decorating a form: sections, rows, grids, tabs, and the elements that show something rather than ask something.

Every container takes a list of elements. None of them has a single-element overload, on purpose: with both available, a graph that passes one element to a list port gets replication instead of a container, and produces N containers of one child each rather than one container of N. Pass a list, even a list of one.
