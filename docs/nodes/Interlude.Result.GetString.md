## In Depth

`Result.GetString(result, key, fallback: "")`

A field's answer as text.

Works on any field, not just text ones: a number comes back as its printed form, a choice as the display name of what was chosen. Handy for building a filename or a parameter value out of whatever the user picked.

A missing or empty field gives the fallback, so the answer is never null and never needs guarding before it is concatenated.

The inputs are:

- `result` (_object_) — The values dictionary or the form output of Form.Show.
- `key` (_string_) — The field to read.
- `fallback` (_string_, defaults to `""`) — Returned when the field is missing or empty.

Returns `value` — The answer as text.

Search terms: `string`, `text`, `get`, `read`.

___
## About the Result nodes

Reading a form's answers.

Every node here accepts either the `values` dictionary or the `form` output of `Form.Show`, so it does not matter which one is to hand. They exist so a graph can say what it expects — a number, a date, a colour — rather than pulling an object out of a dictionary and hoping. Each one takes a fallback used when the field is missing or empty, which is what keeps a downstream node from receiving a null it was not expecting.

___
## Example File

An example graph ships beside this page as `Interlude.Result.GetString.dyn`.

![Result.GetString](https://raw.githubusercontent.com/johnpierson/Interlude/main/docs/nodes/Interlude.Result.GetString_img.png)

The form it builds:

![Result.GetString form](./Interlude.Result.GetString_form.png)
