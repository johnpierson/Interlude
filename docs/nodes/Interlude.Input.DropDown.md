## In Depth

`Input.DropDown(label, items: null, displayNames: null, defaultValue: null, placeholder: "", key: "", tooltip: "", helpText: "")`

A drop-down list. The answer is the selected item itself, not its display name.

The inputs are:

- `label` (_string_) — Caption shown beside the field.
- `items` (_object_, defaults to `null`) — The values to choose between. Can be any objects.
- `displayNames` (_object_, defaults to `null`) — What to show for each item. Falls back to each item's own text.
- `defaultValue` (_object_, defaults to `null`) — Which item starts selected. Null selects the first.
- `placeholder` (_string_, defaults to `""`) — Grey prompt shown while nothing is selected.
- `key` (_string_, defaults to `""`) — Name of this answer in the results. Derived from the label when empty.
- `tooltip` (_string_, defaults to `""`) — Hover text.
- `helpText` (_string_, defaults to `""`) — A line of guidance shown under the field.

Returns `element` — The form element.

Search terms: `dropdown`, `combobox`, `select`, `choose`, `list`, `pick`.
