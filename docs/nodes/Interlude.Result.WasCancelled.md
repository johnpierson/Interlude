## In Depth

`Result.WasCancelled(result)`

Whether the user cancelled or closed the form.

The opposite of `Result.WasSubmitted`, for the graph that reads better as "stop if cancelled" than as "continue if submitted". Closing the window with its X counts as cancelling, as does a run skipped by a false `trigger`.

The inputs are:

- `result` (_object_) — The form output of Form.Show.

Returns `wasCancelled` — True when the form was cancelled.

Search terms: `cancelled`, `canceled`, `closed`, `dismissed`, `escaped`.

___
## About the Result nodes

Reading a form's answers.

Every node here accepts either the `values` dictionary or the `form` output of `Form.Show`, so it does not matter which one is to hand. They exist so a graph can say what it expects — a number, a date, a colour — rather than pulling an object out of a dictionary and hoping. Each one takes a fallback used when the field is missing or empty, which is what keeps a downstream node from receiving a null it was not expecting.
