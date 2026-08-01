## In Depth

`Input.Toggle(label, defaultValue: false, onText: "On", offText: "Off", key: "", tooltip: "", helpText: "")`

An on/off switch. The answer is true or false.

The inputs are:

- `label` (_string_) — Caption shown beside the switch.
- `defaultValue` (_boolean_, defaults to `false`) — Whether the switch starts on.
- `onText` (_string_, defaults to `"On"`) — Wording shown when the switch is on.
- `offText` (_string_, defaults to `"Off"`) — Wording shown when the switch is off.
- `key` (_string_, defaults to `""`) — Name of this answer in the results. Derived from the label when empty.
- `tooltip` (_string_, defaults to `""`) — Hover text.
- `helpText` (_string_, defaults to `""`) — A line of guidance shown under the field.

Returns `element` — The form element.

Search terms: `toggle`, `switch`, `boolean`, `on`, `off`.
