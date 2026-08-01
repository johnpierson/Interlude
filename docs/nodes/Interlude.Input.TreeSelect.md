## In Depth

`Input.TreeSelect(label, nodes: null, allowMultiple: true, defaultValue: null, expandAll: false, key: "", tooltip: "", helpText: "")`

A hierarchy to pick from, built from `Input.TreeItem` nodes.

The inputs are:

- `label` (_string_) — Caption shown beside the field.
- `nodes` (_object_, defaults to `null`) — The root items of the tree.
- `allowMultiple` (_boolean_, defaults to `true`) — Whether several items can be chosen at once.
- `defaultValue` (_object_, defaults to `null`) — Which item or items start selected.
- `expandAll` (_boolean_, defaults to `false`) — Whether every branch starts open.
- `key` (_string_, defaults to `""`) — Name of this answer in the results. Derived from the label when empty.
- `tooltip` (_string_, defaults to `""`) — Hover text.
- `helpText` (_string_, defaults to `""`) — A line of guidance shown under the field.

Returns `element` — The form element.

Search terms: `tree`, `hierarchy`, `nested`, `select`.
