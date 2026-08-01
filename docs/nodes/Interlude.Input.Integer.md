## In Depth

`Input.Integer(label, defaultValue: 0, minimum: null, maximum: null, increment: 1, unit: "", key: "", tooltip: "", helpText: "")`

A whole-number field: counts, quantities, indices — anything a fraction would be nonsense for.

The answer is an integer. Read it with `Result.GetInteger`. This is the node to reach for rather than `Input.Number` with the decimal places set to zero, because that one still hands back 3.0 where this hands back 3, and the difference shows up downstream in list indices and string formatting.

The inputs are:

- `label` (_string_) — Caption shown beside the field.
- `defaultValue` (_integer_, defaults to `0`) — Value the field starts with.
- `minimum` (_object_, defaults to `null`) — Lowest allowed value. Null for no lower bound.
- `maximum` (_object_, defaults to `null`) — Highest allowed value. Null for no upper bound.
- `increment` (_integer_, defaults to `1`) — Step applied by the spinner buttons and arrow keys.
- `unit` (_string_, defaults to `""`) — Suffix shown inside the field.
- `key` (_string_, defaults to `""`) — Name of this answer in the results. Derived from the label when empty.
- `tooltip` (_string_, defaults to `""`) — Hover text.
- `helpText` (_string_, defaults to `""`) — A line of guidance shown under the field.

Returns `element` — The form element.

Search terms: `integer`, `whole`, `count`, `int`.

___
## About the Input nodes

The fields a user answers.

Every input returns an element describing the control, not the control itself, and every one takes the same three trailing options: `key`, which names the answer in the results dictionary; `tooltip`; and `helpText`. Leave `key` empty and it is derived from the label — convenient for a quick form, but give real keys to any graph you intend to keep, because renaming a label would otherwise rename the answer.

Choice inputs take the values themselves, not their display names. Selecting an option hands back the original object — a Revit element, a family type, whatever was put in — so the answer is usable directly instead of needing a lookup back from a string.
