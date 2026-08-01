## In Depth

`Form.Create(title, elements, submitText: "Submit", cancelText: "Cancel", width: 420, maxHeight: 800, formId: "", rememberValues: true, headlessUseDefaults: false, theme: null, options: null)`

Builds a form without showing it, for saving to JSON or for showing later.

Splitting building from showing is what makes a form a document. Build it here, write it with `Form.ToJson`, and the definition can be reviewed in a pull request, diffed between releases and loaded by a graph that did not build it.

It is also the way to check a form without a window appearing: feed the result to `Form.Check`. Nothing is drawn and nothing is remembered until it reaches `Form.ShowDefinition`.

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

___
## About the Form nodes

Showing a form and getting the answers back.

A note on re-execution, because it surprises everyone once: Dynamo re-runs a graph whenever anything upstream changes, and a node that shows a dialog will show it again. Interlude does not pretend otherwise — it gives you the tools to control it. The `trigger` port skips the dialog and returns the last answers when it is false, so a form can be gated behind a button or a boolean. A form already on screen is never opened twice: a second execution waits for the first window and returns its result rather than stacking dialogs. And Manual run mode remains the right setting for any graph built around a form.
