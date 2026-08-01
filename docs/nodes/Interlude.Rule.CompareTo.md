## In Depth

`Rule.CompareTo(otherKey, operation: "GreaterThan", message: "")`

The field's answer must compare correctly against another field, for rules such as "end date must be after start date".

The inputs are:

- `otherKey` (_string_) — The field to compare against.
- `operation` (_string_, defaults to `"GreaterThan"`) — Equals, NotEquals, GreaterThan, GreaterThanOrEqual, LessThan or LessThanOrEqual.
- `message` (_string_, defaults to `""`) — Wording shown when the comparison fails.

Returns `rule` — The rule.

Search terms: `compare`, `other field`, `after`, `before`, `greater`, `cross field`.

___
## About the Rule nodes

Checks applied to a field's answer, for use with `Behavior.WithValidation`.

Rules run as the user types and block submission while any of them fails. A rule on a field the user cannot see is never applied — a hidden field can never stop a form being submitted, which would otherwise mean an error with no control to fix it.

Except for `Rule.Required`, every rule passes on an empty field. Emptiness is `Behavior.Required`'s business, so an optional field with a range on it stays optional.
