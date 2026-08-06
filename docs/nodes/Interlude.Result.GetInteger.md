## In Depth

`Result.GetInteger(result, key, fallback: 0)`

A field's answer as a whole number.

A fractional answer is rounded rather than truncated, so 2.6 becomes 3. Use this for anything that indexes or counts, where a number carrying a hidden .0 causes trouble downstream.

The inputs are:

- `result` (_object_) — The values dictionary or the form output of Form.Show.
- `key` (_string_) — The field to read.
- `fallback` (_integer_, defaults to `0`) — Returned when the field is missing or is not a number.

Returns `value` — The answer as a whole number.

Search terms: `integer`, `int`, `whole`, `count`, `get`, `read`.

___
## About the Result nodes

Reading a form's answers.

Every node here accepts either the `values` dictionary or the `form` output of `Form.Show`, so it does not matter which one is to hand. They exist so a graph can say what it expects — a number, a date, a colour — rather than pulling an object out of a dictionary and hoping. Each one takes a fallback used when the field is missing or empty, which is what keeps a downstream node from receiving a null it was not expecting.

___
## Example File

An example graph ships beside this page as `Interlude.Result.GetInteger.dyn`.

![Result.GetInteger](https://raw.githubusercontent.com/johnpierson/Interlude/main/docs/nodes/Interlude.Result.GetInteger_img.png)

The form it builds:

![Result.GetInteger form](./Interlude.Result.GetInteger_form.png)
