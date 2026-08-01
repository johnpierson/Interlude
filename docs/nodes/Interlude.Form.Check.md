## In Depth

`Form.Check(form)`

Reports the problems Interlude can see in a form without showing it: conditions that name a field that does not exist, duplicate keys, and computed values that depend on each other in a loop.

The inputs are:

- `form` (_FormDefinition_) — The form to check.

The outputs are:

- `isValid` — True when nothing was found.
- `messages` — What was found, if anything.

Search terms: `validate`, `check`, `lint`, `problems`, `warnings`, `debug`.
