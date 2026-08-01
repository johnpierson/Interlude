## In Depth

`Result.ValueByKey(result, key, fallback: null)`

The raw answer for a field, exactly as the form produced it.

The escape hatch. Every other node here promises a type; this one promises nothing and hands over whatever is there — which is what you want for a choice input holding Revit elements, where converting to anything would lose them.

For everything else prefer the typed accessors. `Result.GetNumber` on a field the user left empty gives you the fallback you chose; this gives you whatever emptiness looked like, and the node three steps downstream is where you find out.

The inputs are:

- `result` (_object_) — The values dictionary or the form output of Form.Show.
- `key` (_string_) — The field to read.
- `fallback` (_object_, defaults to `null`) — Returned when the field is missing.

Returns `value` — The answer.

Search terms: `value`, `get`, `key`, `read`, `answer`.

___
## About the Result nodes

Reading a form's answers.

Every node here accepts either the `values` dictionary or the `form` output of `Form.Show`, so it does not matter which one is to hand. They exist so a graph can say what it expects — a number, a date, a colour — rather than pulling an object out of a dictionary and hoping. Each one takes a fallback used when the field is missing or empty, which is what keeps a downstream node from receiving a null it was not expecting.
