## In Depth

`Result.GetList(result, key)`

A field's answer as a list. A single answer comes back as a one-item list, so a downstream node never has to care whether the field allowed several.

The inputs are:

- `result` (_object_) — The values dictionary or the form output of Form.Show.
- `key` (_string_) — The field to read.

Returns `values` — The answers as a list.

Search terms: `list`, `multiple`, `selection`, `items`, `get`.
