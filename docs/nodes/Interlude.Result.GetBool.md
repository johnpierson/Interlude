## In Depth

`Result.GetBool(result, key, fallback: false)`

A field's answer as true or false.

Reads a tick box or a switch directly, and also makes sense of the text and numbers a loaded form might carry — "true", "yes", "1" are true; "false", "no", "0" are false. Anything it cannot make sense of gives the fallback.

This is what gates the rest of a graph, so choose the fallback as the safe answer: the value you would want if the field turned out not to be there at all.

The inputs are:

- `result` (_object_) — The values dictionary or the form output of Form.Show.
- `key` (_string_) — The field to read.
- `fallback` (_boolean_, defaults to `false`) — Returned when the field is missing.

Returns `value` — The answer as a boolean.

Search terms: `bool`, `boolean`, `true`, `false`, `checkbox`, `get`.

___
## About the Result nodes

Reading a form's answers.

Every node here accepts either the `values` dictionary or the `form` output of `Form.Show`, so it does not matter which one is to hand. They exist so a graph can say what it expects — a number, a date, a colour — rather than pulling an object out of a dictionary and hoping. Each one takes a fallback used when the field is missing or empty, which is what keeps a downstream node from receiving a null it was not expecting.

___
## Example File

An example graph ships beside this page as `Interlude.Result.GetBool.dyn`.

![Result.GetBool](https://raw.githubusercontent.com/johnpierson/Interlude/main/docs/nodes/Interlude.Result.GetBool_img.png)

The form it builds:

![Result.GetBool form](./Interlude.Result.GetBool_form.png)
