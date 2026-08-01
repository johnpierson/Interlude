## In Depth

`Input.Number(label, defaultValue: 0, minimum: null, maximum: null, increment: 1, decimalPlaces: 2, unit: "", key: "", tooltip: "", helpText: "")`

A decimal number field.

The inputs are:

- `label` (_string_) — Caption shown beside the field.
- `defaultValue` (_number_, defaults to `0`) — Value the field starts with.
- `minimum` (_object_, defaults to `null`) — Lowest allowed value. Null for no lower bound.
- `maximum` (_object_, defaults to `null`) — Highest allowed value. Null for no upper bound.
- `increment` (_number_, defaults to `1`) — Step applied by the spinner buttons and arrow keys.
- `decimalPlaces` (_integer_, defaults to `2`) — Digits shown after the decimal separator.
- `unit` (_string_, defaults to `""`) — Suffix shown inside the field, such as "mm".
- `key` (_string_, defaults to `""`) — Name of this answer in the results. Derived from the label when empty.
- `tooltip` (_string_, defaults to `""`) — Hover text.
- `helpText` (_string_, defaults to `""`) — A line of guidance shown under the field.

Returns `element` — The form element.

Search terms: `number`, `double`, `decimal`, `numeric`, `value`.
