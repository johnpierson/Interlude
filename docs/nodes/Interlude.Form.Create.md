## In Depth

`Form.Create(title, elements, submitText: "Submit", cancelText: "Cancel", width: 420, maxHeight: 800, formId: "", rememberValues: true, headlessUseDefaults: false, theme: null, options: null)`

Builds a form without showing it, for saving to JSON or for showing later.

The inputs are:

- `title` (_string_) — Shown in the window's title bar.
- `elements` (_list of object_) — The form's contents, built with the Input and Layout nodes.
- `submitText` (_string_, defaults to `"Submit"`) — Caption of the confirm button.
- `cancelText` (_string_, defaults to `"Cancel"`) — Caption of the cancel button.
- `width` (_number_, defaults to `420`) — Window width in pixels.
- `maxHeight` (_number_, defaults to `800`) — Height at which the form starts scrolling.
- `formId` (_string_, defaults to `""`) — Identifies this form across runs.
- `rememberValues` (_boolean_, defaults to `true`) — Pre-fill with the last answers.
- `headlessUseDefaults` (_boolean_, defaults to `false`) — Return defaults instead of stopping when there is no UI.
- `theme` (_object_, defaults to `null`) — Built with the Theme nodes.
- `options` (_object_, defaults to `null`) — Built with Form.Options.

Returns `form` — The form definition.

Search terms: `form`, `create`, `build`, `definition`, `template`.
