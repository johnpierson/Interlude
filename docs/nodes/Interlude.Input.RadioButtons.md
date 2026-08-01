## In Depth

`Input.RadioButtons(label, items: null, displayNames: null, defaultValue: null, horizontal: false, key: "", tooltip: "", helpText: "")`

A set of mutually exclusive radio buttons.

The inputs are:

- `label` (_string_) — Caption shown beside the field.
- `items` (_object_, defaults to `null`) — The values to choose between. Can be any objects.
- `displayNames` (_object_, defaults to `null`) — What to show for each item. Falls back to each item's own text.
- `defaultValue` (_object_, defaults to `null`) — Which item starts selected. Null selects the first.
- `horizontal` (_boolean_, defaults to `false`) — Lay the buttons out in a row instead of a column.
- `key` (_string_, defaults to `""`) — Name of this answer in the results. Derived from the label when empty.
- `tooltip` (_string_, defaults to `""`) — Hover text.
- `helpText` (_string_, defaults to `""`) — A line of guidance shown under the field.

Returns `element` — The form element.

Search terms: `radio`, `option`, `choice`, `exclusive`.
