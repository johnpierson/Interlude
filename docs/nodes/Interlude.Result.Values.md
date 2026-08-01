## In Depth

`Result.Values(result)`

Every answer, in the same order as `Result.Keys`.

The two lists line up index for index, so zipping them gives name-and-answer pairs — which is how you write every answer to a parameter, or a log, without naming the fields one by one in the graph.

The answers come back as they are, untyped, so a graph that needs a particular type should name the field and use the accessor for it.

The inputs are:

- `result` (_object_) — The values dictionary or the form output of Form.Show.

Returns `values` — The answers.

Search terms: `values`, `answers`, `list`.

___
## About the Result nodes

Reading a form's answers.

Every node here accepts either the `values` dictionary or the `form` output of `Form.Show`, so it does not matter which one is to hand. They exist so a graph can say what it expects — a number, a date, a colour — rather than pulling an object out of a dictionary and hoping. Each one takes a fallback used when the field is missing or empty, which is what keeps a downstream node from receiving a null it was not expecting.
