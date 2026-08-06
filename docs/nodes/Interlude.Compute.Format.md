## In Depth

`Compute.Format(template)`

Fills field values into a template: `"Hello {firstName} {lastName}"`. Write a literal brace by doubling it.

Add a colon to say how a value should look: `"Total: £{total:0.00}"` gives two decimal places, `{total:#,0.00}` adds thousands separators, and `{when:d}` or `{when:HH:mm}` turn a date into a date or a time instead of the full timestamp. The wording after the colon is a standard .NET format string, and anything the runtime cannot use is ignored rather than treated as an error.

Without a colon a number is shown the way a person would write it, so a total that landed on `0.30000000000000004` reads as `0.3`. Say `{total:0.00}` when the number of decimal places matters — a price with two of them should not shorten to `£5.5`.

The inputs are:

- `template` (_string_) — The text, with field keys in braces.

Returns `computation` — The computation.

Search terms: `format`, `template`, `interpolate`, `text`, `concat`, `string`, `decimals`, `currency`, `rounding`.

___
## About the Compute nodes

Values worked out from other answers, for use with `Behavior.WithComputed`.

A computed field is driven by the form rather than by the user: it recalculates whenever anything it reads changes, in dependency order, so a total built on a subtotal is always consistent. Computed values that depend on each other in a loop are rejected when the form is built, before a window appears.
