## In Depth

`Input.ColorPicker(label, defaultValue: "#000000", showAlpha: false, presets: null, key: "", tooltip: "", helpText: "")`

A colour field. The answer is an Interlude colour; use `Result.GetColor` to read it as a hex string or as red, green, blue and alpha numbers.

The inputs are:

- `label` (_string_) — Caption shown beside the field.
- `defaultValue` (_string_, defaults to `"#000000"`) — Starting colour, as hex such as "#3366CC".
- `showAlpha` (_boolean_, defaults to `false`) — Add an opacity slider.
- `presets` (_object_, defaults to `null`) — Hex colours offered as swatches above the picker.
- `key` (_string_, defaults to `""`) — Name of this answer in the results. Derived from the label when empty.
- `tooltip` (_string_, defaults to `""`) — Hover text.
- `helpText` (_string_, defaults to `""`) — A line of guidance shown under the field.

Returns `element` — The form element.

Search terms: `colour`, `color`, `swatch`, `rgb`, `hex`, `picker`.
