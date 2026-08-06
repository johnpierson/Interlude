## In Depth

`Result.HasKey(result, key)`

Whether the form has a field with this name. Useful when a graph reads a form loaded from JSON that it did not build itself.

The inputs are:

- `result` (_object_) — The values dictionary or the form output of Form.Show.
- `key` (_string_) — The field to look for.

Returns `exists` — True when the field is present.

Search terms: `has`, `contains`, `exists`, `key`, `field`.

___
## About the Result nodes

Reading a form's answers.

Every node here accepts either the `values` dictionary or the `form` output of `Form.Show`, so it does not matter which one is to hand. They exist so a graph can say what it expects — a number, a date, a colour — rather than pulling an object out of a dictionary and hoping. Each one takes a fallback used when the field is missing or empty, which is what keeps a downstream node from receiving a null it was not expecting.

___
## Example File

An example graph ships beside this page as `Interlude.Result.HasKey.dyn`.

![Result.HasKey](https://raw.githubusercontent.com/johnpierson/Interlude/main/docs/nodes/Interlude.Result.HasKey_img.png)

The form it builds:

![Result.HasKey form](./Interlude.Result.HasKey_form.png)
