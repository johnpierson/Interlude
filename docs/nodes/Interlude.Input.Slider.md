## In Depth

`Input.Slider(label, minimum: 0, maximum: 100, defaultValue: 0, step: 1, decimalPlaces: 2, key: "", tooltip: "", helpText: "")`

A number chosen by dragging along a track.

The inputs are:

- `label` (_string_) — Caption shown beside the field.
- `minimum` (_number_, defaults to `0`) — Left end of the track.
- `maximum` (_number_, defaults to `100`) — Right end of the track.
- `defaultValue` (_number_, defaults to `0`) — Value the slider starts at.
- `step` (_number_, defaults to `1`) — Snap increment. Zero for continuous.
- `decimalPlaces` (_integer_, defaults to `2`) — Digits shown in the readout.
- `key` (_string_, defaults to `""`) — Name of this answer in the results. Derived from the label when empty.
- `tooltip` (_string_, defaults to `""`) — Hover text.
- `helpText` (_string_, defaults to `""`) — A line of guidance shown under the field.

Returns `element` — The form element.

Search terms: `slider`, `range`, `drag`, `number`.
