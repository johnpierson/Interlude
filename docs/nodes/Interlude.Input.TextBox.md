## In Depth

`Input.TextBox(label, defaultValue: "", placeholder: "", key: "", tooltip: "", helpText: "")`

A single-line text field. The workhorse: names, prefixes, codes, anything typed.

The answer is always a string and never null — an untouched field returns its default, and a field the user cleared returns an empty string. Read it with `Result.GetString`.

Use `Input.TextArea` when the answer runs to more than a line, and attach `Rule.Regex` or `Rule.Length` with `Behavior.WithValidation` when the text has to take a particular shape. Checking the format after the form closes is too late to tell the user anything useful.

The inputs are:

- `label` (_string_) — Caption shown beside the field.
- `defaultValue` (_string_, defaults to `""`) — Value the field starts with.
- `placeholder` (_string_, defaults to `""`) — Grey prompt shown while the field is empty.
- `key` (_string_, defaults to `""`) — Name of this answer in the results. Derived from the label when empty.
- `tooltip` (_string_, defaults to `""`) — Hover text.
- `helpText` (_string_, defaults to `""`) — A line of guidance shown under the field.

Returns `element` — The form element.

Search terms: `text`, `string`, `textbox`, `input`, `field`.

___
## About the Input nodes

The fields a user answers.

Every input returns an element describing the control, not the control itself, and every one takes the same three trailing options: `key`, which names the answer in the results dictionary; `tooltip`; and `helpText`. Leave `key` empty and it is derived from the label — convenient for a quick form, but give real keys to any graph you intend to keep, because renaming a label would otherwise rename the answer.

Choice inputs take the values themselves, not their display names. Selecting an option hands back the original object — a Revit element, a family type, whatever was put in — so the answer is usable directly instead of needing a lookup back from a string.
