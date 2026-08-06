# Authoring notes

What the schema cannot tell you: which control to reach for, and which of two correct forms is
the better one. [schema.md](schema.md) is the authority on what exists — this is the authority on
what to do with it.

## Choosing a control

The question is what the answer *is*, not what the control looks like.

| The answer is | Use |
| --- | --- |
| Free text, one line | `textBox` |
| Free text, several lines | `textBox` with `isMultiline` |
| A secret | `password` — but see the warning under *Re-execution* |
| A number with a meaningful exact value | `numeric`, or `integer` for whole ones |
| A number where the *feel* matters more than the digits | `slider` |
| One of a handful of choices, all worth seeing | `radioGroup` |
| One of many choices | `dropdown`, with `showSearch` past about fifteen |
| Yes or no, as a statement the user agrees with | `checkBox` with `content` |
| Yes or no, as a setting being switched | `toggle` |
| Several of a list | `listSelection` |
| One or several from a hierarchy | `treeSelection` |
| A date | `datePicker` |
| A colour | `colorPicker` |
| A path | `filePicker` or `folderPicker` |

Two that get confused: **`radioGroup` versus `dropdown`** is about how many options, not how
important the choice is — three options in a drop-down hide two of them for no reason, and twelve
radio buttons are a wall. **`checkBox` versus `toggle`** is about grammar: a check box reads as a
sentence the user is agreeing with ("Include sheets"), a toggle as a thing being turned on.

Prefer `content` on a check box to a separate `label`. "Include sheets" beside the box reads
better than an empty label column and the words floating to its right.

## Keys

The key is the name the answer arrives under, and it is the only part of the form the rest of the
graph depends on. Set it explicitly on everything that produces a value.

Leave it out and it is a slug of the label, so the label becomes load-bearing: fixing a typo in
"Wall Type" renames `wall_type` and every `Result.GetString(values, "wall_type")` downstream
quietly starts returning nothing. That is the single most common way a working form breaks.

Two fields sharing a key do not error. The second silently becomes `prefix_2`, which is a bug you
will find in the graph rather than in the form.

Use `snake_case`, because that is what a slugged label produces and a form should not be half one
convention and half another.

## Layout

Reach for a container when it means something, not to decorate:

- **`groupBox`** — a set of fields that belong together, with a heading. The default answer.
- **`card`** — the same, without the heading, when the grouping is visual.
- **`expander`** — advanced options that most runs leave alone. Collapsed by default, or it is
  just a group box that moves.
- **`tabs`** — a form with genuinely separate pages. Two tabs is usually one form with a group box.
- **`hStack`** — fields that read as one line: a width and a height, a from and a to.
- **`grid`** — a real table of fields. Rarely what you want.

Order fields the way the user thinks about the job, which is usually the order they would say
them out loud. Put the field that changes what the rest of the form asks near the top, so the form
does not rearrange itself under the cursor.

## Behaviour

**Hide with `visibleIf`, do not disable.** A disabled field is a promise that it might become
relevant; if it never will, it is clutter. `visibleIf` removes the element entirely and it takes
up no space. Use `enabledIf` for the narrower case where the field is relevant but not yet
answerable — a folder picker beside the check box that turns it on.

**`requiredIf` over `required` when a field is conditional.** A required field that is hidden is
not applied, which is the behaviour you want and is worth knowing before you rely on it.

**Computed values make the form own a field.** A total, a derived code, a preview of a name the
graph will build. They recalculate in dependency order, and a loop between them is rejected when
the form is built rather than discovered as a hang. Set `isReadOnly` on anything computed unless
the user is genuinely allowed to override it.

**Put a format specifier on any number a `format` template shows.** Write `{total:0.00}`, not
`{total}`. Arithmetic on doubles lands on values like `655.2000000000001`, and while a bare
placeholder now rounds that to something readable, only the specifier pins the number of decimal
places — money shown as `£5.5` is the failure this prevents. The specifier goes after the first
colon and is a standard .NET format string: `#,0.00` for thousands separators, `0%` for a fraction
shown as a percentage, `yyyy-MM-dd` or `HH:mm` for a date field, which otherwise renders as a full
ISO timestamp. A specifier the runtime cannot use is ignored rather than raised as an error, so
this is safe to reach for.

**Validation runs while the user types.** A `regex` rule wants a `message` that says what the
right shape is — "Use the form ABC-1234." — not one that says the value is invalid, which the
user can already see.

## Options that are not in the file

A drop-down whose options are Revit elements — levels, view templates, families — cannot be a
document. The elements do not exist in another model, and saving one produces a file that loads
with the options degraded to `{ "$opaque": "Wall <312840>" }`: text, not elements.

So when the options come from the model, do not invent them. Either:

- leave the input's `options` empty and give the field an explicit `key`, because the graph fills
  it in by that key with `Form.WithOptions` between loading the form and showing it, or
- ask the user for the fixed list, if it really is fixed.

Say which you did. A form full of plausible invented level names is worse than an empty one,
because it looks finished.

The wiring for the first, which is worth putting in the reply whenever a field is left empty:

```
Form.FromJson ──► Form.WithOptions(key: "levels", items: levels, displayNames: names)
              ──► Form.ShowDefinition
```

One `Form.WithOptions` per model-driven field, chained. The elements go in whole and the chosen one
comes back as the element, so nothing downstream has to look it up by name. The key in the file and
the key on that node have to match exactly — which is the reason to write the key rather than let it
be derived from the label, where changing the wording would quietly rename it.

A `defaultValue` on a field the graph fills in is pointless: it names an option that will not be
there, and Interlude drops it. Leave it out.

## The theme

Name a preset. `"theme": { "preset": "neubrutalist" }` or `"classic"`, with `"mode"` as `auto`,
`light` or `dark`. The palette travels as that name, so a form checked into a repository does not
show an eighteen-colour diff every time the built-in colours are tuned.

Write `lightPalette` and `darkPalette` out only when the user has actually chosen their own
colours, and then write them completely — a palette with three colours filled in and fifteen
missing has no defined appearance.

The default is neubrutalist and light. It is a loud default on purpose. Do not quietly switch a
form to `classic` because it seems safer; if the user has not said, leave the theme out entirely
and let the default apply.

## Cancelling, and running unattended

A cancelled form returns **every field's default** with `wasSubmitted` false — never nulls. Two
consequences worth designing around:

- Defaults should be values the graph can survive being handed. A default that would delete
  something is a bad default.
- Anything with consequences should be gated on `wasSubmitted`. Mention this in your reply when
  the form drives something destructive; it is the difference between a cancelled dialog and a
  cancelled operation.

`headlessUseDefaults` decides what happens with no UI available — a scheduled run, a command
line. False, the default, throws with an explanation. True returns the defaults and carries on.
Set it true only when the user has said the graph runs unattended.

## Re-execution

Dynamo re-runs a graph whenever anything upstream changes, and a node that shows a dialog shows it
again. The form file cannot fix this; the graph can, with the `trigger` port on
`Form.ShowDefinition`. `rememberValues` — true by default — re-opens the form with the last
submitted answers, and a cancelled run never overwrites them.

Remembered answers live in memory for the lifetime of the Dynamo process. They do not survive a
restart, and they are not written anywhere.

**A `password` field is remembered like any other**, so a submitted secret sits in the process
until Dynamo closes and is re-filled into the form on the next run. Set `rememberValues` to false
on any form that asks for one, and say why when you do.

Set `formId` on any form worth remembering. Without one it is derived from the form's shape, so
editing the form loses its remembered answers.

## Size

`window.width` defaults to 420, which suits a single column of fields. Widen it for forms with
side-by-side fields or long option text; do not widen it to make a short form look substantial.
`maxHeight` defaults to 800 and the form scrolls past it, so a long form does not need anything
doing to it.
