## In Depth

`Behavior.RequiredIf(element, condition, message: "")`

Makes the element required only while the condition holds.

The inputs are:

- `element` (_FormElement_) — The element to control.
- `condition` (_ConditionExpr_) — Built with the Condition nodes.
- `message` (_string_, defaults to `""`) — Wording shown when the field is left empty.

Returns `element` — A copy of the element with the condition attached.

Search terms: `required`, `mandatory`, `conditional`, `requiredif`.
