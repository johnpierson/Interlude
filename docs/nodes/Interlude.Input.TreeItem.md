## In Depth

`Input.TreeItem(displayName, value: null, children: null, expanded: false, selectable: true)`

One item of a tree, for `Input.TreeSelect`.

Build a hierarchy by feeding items into a parent's `children` port, as deep as you like. This is the only Input node that does not produce a form element on its own: it is the material `Input.TreeSelect` is made of, and placing it anywhere else does nothing.

`value` is what selecting the item returns, and falls back to the display name when left empty — so a tree of plain strings needs nothing else, while a tree of Revit elements carries them through untouched.

The inputs are:

- `displayName` (_string_) — What the user reads.
- `value` (_object_, defaults to `null`) — What choosing this item returns. Falls back to the display name.
- `children` (_list of object_, defaults to `null`) — Items nested beneath this one.
- `expanded` (_boolean_, defaults to `false`) — Whether this item starts open.
- `selectable` (_boolean_, defaults to `true`) — False for a branch that only groups its children.

Returns `treeItem` — The tree item.

Search terms: `tree`, `item`, `node`, `branch`, `leaf`.

___
## About the Input nodes

The fields a user answers.

Every input returns an element describing the control, not the control itself, and every one takes the same three trailing options: `key`, which names the answer in the results dictionary; `tooltip`; and `helpText`. Leave `key` empty and it is derived from the label — convenient for a quick form, but give real keys to any graph you intend to keep, because renaming a label would otherwise rename the answer.

Choice inputs take the values themselves, not their display names. Selecting an option hands back the original object — a Revit element, a family type, whatever was put in — so the answer is usable directly instead of needing a lookup back from a string.
