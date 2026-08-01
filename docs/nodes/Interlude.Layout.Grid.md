## In Depth

`Layout.Grid(elements, columns: "*, *", columnSpacing: -1, rowSpacing: -1)`

Elements arranged in a grid. Columns are described as a comma-separated list, where `auto` sizes to content, `*` takes a share of the leftover space, `2*` takes two shares, and a plain number is a pixel width: `"auto, *, 120"`.

Elements fill the grid in order unless they were placed with `Layout.Cell`.

The inputs are:

- `elements` (_list of FormElement_) — What goes inside.
- `columns` (_string_, defaults to `"*, *"`) — Column widths, comma separated.
- `columnSpacing` (_number_, defaults to `-1`) — Gap between columns. Negative uses the theme's spacing.
- `rowSpacing` (_number_, defaults to `-1`) — Gap between rows. Negative uses the theme's spacing.

Returns `element` — The form element.

Search terms: `grid`, `table`, `columns`, `rows`, `layout`.
