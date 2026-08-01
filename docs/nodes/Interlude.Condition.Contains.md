## In Depth

`Condition.Contains(key, value, ignoreCase: false)`

True when the field contains the value: as a substring for text, or as a member for a multi-select answer.

The inputs are:

- `key` (_string_) — The field to read.
- `value` (_object_) — What to look for.
- `ignoreCase` (_boolean_, defaults to `false`) — Ignore letter case when comparing text.

Returns `condition` — The condition.

Search terms: `contains`, `includes`, `has`, `substring`.
