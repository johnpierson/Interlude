## In Depth

`Form.ShowDefinition(form, trigger: true)`

Shows a form that was built with `Form.Create` or loaded from JSON.

The same dialog as `Form.Show`, and the same four outputs, differing only in where the form came from: a definition rather than a list of elements. This is the show half of the document workflow — `Form.FromJson` then this, and a graph runs a form maintained somewhere else entirely.

Everything about re-execution applies here identically: the `trigger` port, the re-entrancy latch, and remembered answers.

The inputs are:

- `form` (_FormDefinition_) — The form to show.
- `trigger` (_object_, defaults to `true`) — Set to false to skip the dialog and return the last answers.

The outputs are:

- `values` — The answers, keyed by field.
- `wasSubmitted` — True when the user confirmed rather than cancelled.
- `buttonClicked` — Which button ended the form.
- `form` — The full result, for the Result nodes.

Search terms: `form`, `show`, `definition`, `json`, `dialog`.

___
## About the Form nodes

Showing a form and getting the answers back.

A note on re-execution, because it surprises everyone once: Dynamo re-runs a graph whenever anything upstream changes, and a node that shows a dialog will show it again. Interlude does not pretend otherwise — it gives you the tools to control it. The `trigger` port skips the dialog and returns the last answers when it is false, so a form can be gated behind a button or a boolean. A form already on screen is never opened twice: a second execution waits for the first window and returns its result rather than stacking dialogs. And Manual run mode remains the right setting for any graph built around a form.
