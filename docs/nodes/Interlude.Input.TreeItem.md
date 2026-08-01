## In Depth

`Input.TreeItem(displayName, value: null, children: null, expanded: false, selectable: true)`

One item of a tree, for `Input.TreeSelect`. Nest these to build a hierarchy.

The inputs are:

- `displayName` (_string_) — What the user reads.
- `value` (_object_, defaults to `null`) — What choosing this item returns. Falls back to the display name.
- `children` (_object_, defaults to `null`) — Items nested beneath this one.
- `expanded` (_boolean_, defaults to `false`) — Whether this item starts open.
- `selectable` (_boolean_, defaults to `true`) — False for a branch that only groups its children.

Returns `treeItem` — The tree item.

Search terms: `tree`, `item`, `node`, `branch`, `leaf`.
