## In Depth

`Form.Show(title, elements, trigger: true, submitText: "Submit", cancelText: "Cancel", width: 420, maxHeight: 800, formId: "", rememberValues: true, headlessUseDefaults: false, theme: null, options: null)`

Shows a form and waits for the user to answer it.

Cancelling returns every field's default value rather than nulls, with `wasSubmitted` false. Check `wasSubmitted` before acting on the answers; you never need to null-check the values themselves.

The inputs are:

- `title` (_string_) — Shown in the window's title bar.
- `elements` (_list of object_) — The form's contents, built with the Input and Layout nodes.
- `trigger` (_object_, defaults to `true`) — Set to false to skip the dialog and return the last answers for this form. Anything else, including true, shows it. Doubles as a sequencing input.
- `submitText` (_string_, defaults to `"Submit"`) — Caption of the confirm button.
- `cancelText` (_string_, defaults to `"Cancel"`) — Caption of the cancel button.
- `width` (_number_, defaults to `420`) — Window width in pixels.
- `maxHeight` (_number_, defaults to `800`) — Height at which the form starts scrolling.
- `formId` (_string_, defaults to `""`) — Identifies this form across runs, for remembered answers. Derived from the title and field keys when empty.
- `rememberValues` (_boolean_, defaults to `true`) — Pre-fill the form with the last answers it was submitted with.
- `headlessUseDefaults` (_boolean_, defaults to `false`) — What to do with no user interface, as in a command-line or scheduled run. False stops the graph with an explanation; true returns every field's default.
- `theme` (_object_, defaults to `null`) — Built with the Theme nodes. Null uses the system theme.
- `options` (_object_, defaults to `null`) — Built with Form.Options, for the less common settings.

The outputs are:

- `values` — The answers, keyed by field.
- `wasSubmitted` — True when the user confirmed rather than cancelled.
- `buttonClicked` — Which button ended the form.
- `form` — The full result, for the Result nodes.

Search terms: `form`, `show`, `dialog`, `ui`, `prompt`, `ask`, `input`, `data shapes`.
