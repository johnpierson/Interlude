## In Depth

`Input.Number(label, defaultValue: 0, minimum: null, maximum: null, increment: 1, decimalPlaces: 2, unit: "", key: "", tooltip: "", helpText: "")`

A decimal number field, with spinner buttons and an optional unit suffix.

The answer is a number, never a string, so it can go straight into arithmetic. Read it with `Result.GetNumber`.

`minimum` and `maximum` clamp what the field will accept as it is typed, which is not the same as validating it: a value outside the range never gets entered rather than being entered and then complained about. Leave them null for an unbounded field.

`unit` is decoration shown inside the field — it is not converted or appended to the answer, which stays a bare number.

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

___
## About the Input nodes

The fields a user answers.

Every input returns an element describing the control, not the control itself, and every one takes the same three trailing options: `key`, which names the answer in the results dictionary; `tooltip`; and `helpText`. Leave `key` empty and it is derived from the label — convenient for a quick form, but give real keys to any graph you intend to keep, because renaming a label would otherwise rename the answer.

Choice inputs take the values themselves, not their display names. Selecting an option hands back the original object — a Revit element, a family type, whatever was put in — so the answer is usable directly instead of needing a lookup back from a string.

___
## Example File

An example graph ships beside this page as `Interlude.Input.Number.dyn`.

![Input.Number](https://raw.githubusercontent.com/johnpierson/Interlude/main/docs/nodes/Interlude.Input.Number_img.png)

The form it builds:

![Input.Number form](./Interlude.Input.Number_form.png)
