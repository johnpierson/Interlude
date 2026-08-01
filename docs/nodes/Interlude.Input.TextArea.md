## In Depth

`Input.TextArea(label, defaultValue: "", lines: 4, placeholder: "", key: "", tooltip: "", helpText: "")`

A multi-line text field.

The inputs are:

- `label` (_string_) — Caption shown beside the field.
- `defaultValue` (_string_, defaults to `""`) — Value the field starts with.
- `lines` (_integer_, defaults to `4`) — Visible height, in lines.
- `placeholder` (_string_, defaults to `""`) — Grey prompt shown while the field is empty.
- `key` (_string_, defaults to `""`) — Name of this answer in the results. Derived from the label when empty.
- `tooltip` (_string_, defaults to `""`) — Hover text.
- `helpText` (_string_, defaults to `""`) — A line of guidance shown under the field.

Returns `element` — The form element.

Search terms: `text`, `multiline`, `notes`, `paragraph`, `textarea`.
