## In Depth

`Input.DatePicker(label, defaultValue: null, includeTime: false, minimum: null, maximum: null, key: "", tooltip: "", helpText: "")`

A calendar field, optionally with a time of day.

The answer is a DateTime — **or null, when the field is left empty**. This is the one input whose answer can genuinely be nothing, so either attach `Behavior.Required` or handle the empty case downstream. `Result.GetDate` takes a fallback for exactly this.

The field is shown and typed in the machine's own date format, so a user in one region and a user in another see what each expects. Only the display is regional; the value that comes back is a proper date, not the text of one.

To make one date depend on another — an end after a start — use `Rule.CompareTo` with `Behavior.WithValidation`.

The inputs are:

- `label` (_string_) — Caption shown beside the field.
- `defaultValue` (_object_, defaults to `null`) — Date the field starts on.
- `includeTime` (_boolean_, defaults to `false`) — Add a time-of-day box beside the calendar.
- `minimum` (_object_, defaults to `null`) — Earliest selectable date.
- `maximum` (_object_, defaults to `null`) — Latest selectable date.
- `key` (_string_, defaults to `""`) — Name of this answer in the results. Derived from the label when empty.
- `tooltip` (_string_, defaults to `""`) — Hover text.
- `helpText` (_string_, defaults to `""`) — A line of guidance shown under the field.

Returns `element` — The form element.

Search terms: `date`, `calendar`, `time`, `when`, `datetime`.

___
## About the Input nodes

The fields a user answers.

Every input returns an element describing the control, not the control itself, and every one takes the same three trailing options: `key`, which names the answer in the results dictionary; `tooltip`; and `helpText`. Leave `key` empty and it is derived from the label — convenient for a quick form, but give real keys to any graph you intend to keep, because renaming a label would otherwise rename the answer.

Choice inputs take the values themselves, not their display names. Selecting an option hands back the original object — a Revit element, a family type, whatever was put in — so the answer is usable directly instead of needing a lookup back from a string.

___
## Example File

An example graph ships beside this page as `Interlude.Input.DatePicker.dyn`.

![Input.DatePicker](https://raw.githubusercontent.com/johnpierson/Interlude/main/docs/nodes/Interlude.Input.DatePicker_img.png)

The form it builds:

![Input.DatePicker form](./Interlude.Input.DatePicker_form.png)
