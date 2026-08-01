## In Depth

`Form.Forget(formId: "")`

Forgets the answers remembered for a form, so the next run starts from its defaults.

The inputs are:

- `formId` (_string_, defaults to `""`) — The form's id. Empty forgets every form.

Returns `cleared` — True once the answers have been forgotten.

Search terms: `forget`, `clear`, `reset`, `remember`, `cache`.

___
## About the Form nodes

Showing a form and getting the answers back.

A note on re-execution, because it surprises everyone once: Dynamo re-runs a graph whenever anything upstream changes, and a node that shows a dialog will show it again. Interlude does not pretend otherwise — it gives you the tools to control it. The `trigger` port skips the dialog and returns the last answers when it is false, so a form can be gated behind a button or a boolean. A form already on screen is never opened twice: a second execution waits for the first window and returns its result rather than stacking dialogs. And Manual run mode remains the right setting for any graph built around a form.
