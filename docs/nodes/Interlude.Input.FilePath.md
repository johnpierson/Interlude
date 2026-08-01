## In Depth

`Input.FilePath(label, defaultValue: "", filter: "All files|*.*", allowMultiple: false, forSaving: false, key: "", tooltip: "", helpText: "")`

A file path with a Browse button. With `allowMultiple` the answer is a list of paths; otherwise it is a single path string.

The inputs are:

- `label` (_string_) — Caption shown beside the field.
- `defaultValue` (_string_, defaults to `""`) — Path the field starts with.
- `filter` (_string_, defaults to `"All files|*.*"`) — Dialog filter, such as "Revit files|*.rvt|All files|*.*".
- `allowMultiple` (_boolean_, defaults to `false`) — Whether several files can be chosen.
- `forSaving` (_boolean_, defaults to `false`) — Show a save dialog instead of an open dialog.
- `key` (_string_, defaults to `""`) — Name of this answer in the results. Derived from the label when empty.
- `tooltip` (_string_, defaults to `""`) — Hover text.
- `helpText` (_string_, defaults to `""`) — A line of guidance shown under the field.

Returns `element` — The form element.

Search terms: `file`, `path`, `browse`, `open`, `save`, `filepath`.
