## In Depth

`Compute.Arithmetic(left, operation, right)`

Arithmetic on two values. Each side is either a field key or a nested computation. Dividing by zero gives zero rather than infinity, so a half-filled form shows a sensible total instead of a symbol.

The inputs are:

- `left` (_object_) — A field key, a literal, or a nested computation.
- `operation` (_string_) — Add, Subtract, Multiply, Divide, Modulo, Power, Min or Max.
- `right` (_object_) — A field key, a literal, or a nested computation.

Returns `computation` — The computation.

Search terms: `arithmetic`, `math`, `multiply`, `divide`, `subtract`, `calculate`.
