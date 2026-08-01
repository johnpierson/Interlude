## In Depth

`Form.ShowDefinition(form, trigger: true)`

Shows a form that was built with `Form.Create` or loaded from JSON.

The inputs are:

- `form` (_FormDefinition_) — The form to show.
- `trigger` (_object_, defaults to `true`) — Set to false to skip the dialog and return the last answers.

The outputs are:

- `values` — The answers, keyed by field.
- `wasSubmitted` — True when the user confirmed rather than cancelled.
- `buttonClicked` — Which button ended the form.
- `form` — The full result, for the Result nodes.

Search terms: `form`, `show`, `definition`, `json`, `dialog`.
