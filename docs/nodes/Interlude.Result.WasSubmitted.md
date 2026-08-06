## In Depth

`Result.WasSubmitted(result)`

Whether the user confirmed the form rather than cancelling it.

**Test this before acting on the answers.** It is the whole of the cancellation contract: a cancelled form still returns every field's default value rather than nulls, so the answers always look usable and nothing downstream will fail to warn you. What tells the two apart is this flag and nothing else.

The same value comes out of `Form.Show` directly; this node exists for reading it back off the `form` output further down a graph.

The inputs are:

- `result` (_object_) — The form output of Form.Show.

Returns `wasSubmitted` — True when the form was confirmed.

Search terms: `submitted`, `confirmed`, `ok`, `accepted`.

___
## About the Result nodes

Reading a form's answers.

Every node here accepts either the `values` dictionary or the `form` output of `Form.Show`, so it does not matter which one is to hand. They exist so a graph can say what it expects — a number, a date, a colour — rather than pulling an object out of a dictionary and hoping. Each one takes a fallback used when the field is missing or empty, which is what keeps a downstream node from receiving a null it was not expecting.

___
## Example File

An example graph ships beside this page as `Interlude.Result.WasSubmitted.dyn`.

![Result.WasSubmitted](https://raw.githubusercontent.com/johnpierson/Interlude/main/docs/nodes/Interlude.Result.WasSubmitted_img.png)

The form it builds:

![Result.WasSubmitted form](./Interlude.Result.WasSubmitted_form.png)
