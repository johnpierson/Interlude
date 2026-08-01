## In Depth

`Behavior.Required(element, message: "")`

Makes the element always required. The form cannot be submitted while it is empty.

Adds the asterisk beside the label as well as enforcing it, which is why this is the node to use rather than attaching `Rule.Required` by hand: a requirement the user only discovers by pressing Submit is a worse form.

Remember that false and zero are answers. A tick box is never empty, so requiring one does nothing — to insist a box is ticked, use `Rule.Required`'s sibling test through `Behavior.RequiredIf` with `Condition.IsNotChecked`, or simply validate it.

The inputs are:

- `element` (_FormElement_) — The element to control.
- `message` (_string_, defaults to `""`) — Wording shown when the field is left empty.

Returns `element` — A copy of the element, marked required.

Search terms: `required`, `mandatory`, `must`, `asterisk`.

___
## About the Behavior nodes

Adds behaviour to an element: when it is visible, when it is enabled, when it is required, what makes it valid, and what its value is computed from.

Every node here returns a new element rather than changing the one it was given. Elements are values, so the same element can be fed into two different behaviours without one of them affecting the other, and re-running a graph rebuilds the tree from scratch with nothing left over from last time.
