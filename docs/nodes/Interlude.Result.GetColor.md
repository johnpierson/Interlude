## In Depth

`Result.GetColor(result, key)`

A colour answer, broken out as hex and as numbers on separate ports.

Both forms come out at once — the hex string, and red, green, blue and alpha as numbers from 0 to 255 — because the node that wants a colour next might want either, and converting between them in a graph is tedious.

Take the numbers to build a Revit or Dynamo colour; take the hex to write a parameter, a filename or a stylesheet.

The inputs are:

- `result` (_object_) — The values dictionary or the form output of Form.Show.
- `key` (_string_) — The field to read.

The outputs are:

- `hex` — The colour as "#RRGGBB", or "#AARRGGBB" when it is not fully opaque.
- `red` — Red, 0 to 255.
- `green` — Green, 0 to 255.
- `blue` — Blue, 0 to 255.
- `alpha` — Opacity, 0 to 255.

Search terms: `colour`, `color`, `hex`, `rgb`, `argb`, `get`, `read`.

___
## About the Result nodes

Reading a form's answers.

Every node here accepts either the `values` dictionary or the `form` output of `Form.Show`, so it does not matter which one is to hand. They exist so a graph can say what it expects — a number, a date, a colour — rather than pulling an object out of a dictionary and hoping. Each one takes a fallback used when the field is missing or empty, which is what keeps a downstream node from receiving a null it was not expecting.
