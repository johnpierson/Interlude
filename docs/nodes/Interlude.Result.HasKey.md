## In Depth

`Result.HasKey(result, key)`

Whether the form has a field with this name. Useful when a graph reads a form loaded from JSON that it did not build itself.

The inputs are:

- `result` (_object_) — The values dictionary or the form output of Form.Show.
- `key` (_string_) — The field to look for.

Returns `exists` — True when the field is present.

Search terms: `has`, `contains`, `exists`, `key`, `field`.
