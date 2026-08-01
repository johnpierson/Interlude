## In Depth

`Layout.TabPage(header, elements)`

One page of a `Layout.Tabs` strip.

Like `Input.TreeItem`, this is material rather than a standalone element: it only means anything fed into a `Layout.Tabs` node. The header is what the user clicks.

The inputs are:

- `header` (_string_) — The tab's caption.
- `elements` (_list of FormElement_) — What goes on the page.

Returns `element` — The form element.

Search terms: `tab`, `page`, `tabpage`.

___
## About the Layout nodes

Arranging and decorating a form: sections, rows, grids, tabs, and the elements that show something rather than ask something.

Every container takes a list of elements. None of them has a single-element overload, on purpose: with both available, a graph that passes one element to a list port gets replication instead of a container, and produces N containers of one child each rather than one container of N. Pass a list, even a list of one.
