## In Depth

`Layout.Progress(value: 0, maximum: 100, indeterminate: false, segments: 0)`

A progress bar. It shows a fixed value: nothing in the form updates it while it is open.

The inputs are:

- `value` (_number_, defaults to `0`) — How far along, between 0 and the maximum.
- `maximum` (_number_, defaults to `100`) — The value that counts as complete.
- `indeterminate` (_boolean_, defaults to `false`) — Show a looping animation instead of a fixed amount.
- `segments` (_integer_, defaults to `0`) — Draw the bar as this many discrete cells rather than one continuous fill. Zero is continuous. Segments are for counting rather than measuring: "five of seven days" reads off a segmented bar at a glance, where a continuous bar at 71% does not.

Returns `element` — The form element.

Search terms: `progress`, `bar`, `percent`, `loading`, `segments`, `steps`.
