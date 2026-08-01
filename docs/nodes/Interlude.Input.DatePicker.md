## In Depth

`Input.DatePicker(label, defaultValue: null, includeTime: false, minimum: null, maximum: null, key: "", tooltip: "", helpText: "")`

A calendar field. The answer is a DateTime, or null when left empty.

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
