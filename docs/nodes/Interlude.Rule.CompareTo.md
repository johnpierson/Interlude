## In Depth

`Rule.CompareTo(otherKey, operation: "GreaterThan", message: "")`

The field's answer must compare correctly against another field, for rules such as "end date must be after start date".

The inputs are:

- `otherKey` (_string_) — The field to compare against.
- `operation` (_string_, defaults to `"GreaterThan"`) — Equals, NotEquals, GreaterThan, GreaterThanOrEqual, LessThan or LessThanOrEqual.
- `message` (_string_, defaults to `""`) — Wording shown when the comparison fails.

Returns `rule` — The rule.

Search terms: `compare`, `other field`, `after`, `before`, `greater`, `cross field`.
