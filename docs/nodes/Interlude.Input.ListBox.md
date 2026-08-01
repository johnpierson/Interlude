## In Depth

`Input.ListBox(label, items: null, displayNames: null, allowMultiple: true, defaultValue: null, visibleRows: 6, key: "", tooltip: "", helpText: "")`

A list to pick from. With `allowMultiple` the answer is a list of the chosen items; otherwise it is the single chosen item.

The inputs are:

- `label` (_string_) — Caption shown beside the field.
- `items` (_object_, defaults to `null`) — The values to choose between. Can be any objects.
- `displayNames` (_object_, defaults to `null`) — What to show for each item. Falls back to each item's own text.
- `allowMultiple` (_boolean_, defaults to `true`) — Whether several items can be chosen at once.
- `defaultValue` (_object_, defaults to `null`) — Which item or items start selected.
- `visibleRows` (_integer_, defaults to `6`) — How many rows are shown before the list scrolls.
- `key` (_string_, defaults to `""`) — Name of this answer in the results. Derived from the label when empty.
- `tooltip` (_string_, defaults to `""`) — Hover text.
- `helpText` (_string_, defaults to `""`) — A line of guidance shown under the field.

Returns `element` — The form element.

Search terms: `listbox`, `list`, `multiselect`, `select`, `choose`.
