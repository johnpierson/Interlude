# Forms as JSON

`Form.ToJson` and `Form.FromJson` round-trip a form losslessly. This is the package's strategic
contract, not a convenience: once a form is data, it can be checked into a repository, reviewed
in a pull request, diffed between releases, replayed in a test, handed to the preview harness,
and one day rendered by something other than WPF.

Worked examples live in [`samples/`](../samples/). The test suite validates every one of them
against the schema on each build, so they cannot drift.

## Shape

```json
{
  "schemaVersion": 1,
  "title": "Rename views",
  "description": "Optional paragraph above the first field.",
  "formId": "acme.rename-views",
  "rememberValues": true,
  "headlessUseDefaults": false,
  "elements": [ ... ],
  "buttons": { "submitText": "Submit", "cancelText": "Cancel", "showCancel": true },
  "window": { "width": 420, "maxHeight": 800, "isResizable": true },
  "theme": { "mode": "auto", "density": "comfortable", "cornerRadius": 4 }
}
```

`schemaVersion` is checked before anything else is read. A file written by a newer Interlude is
refused with an explanation rather than partly understood.

## Elements

Every element carries a `$type` discriminator naming its kind, plus the properties shared by all
of them: `key`, `label`, `tooltip`, `helpText`, `visibleIf`, `enabledIf`, `requiredIf`, `rules`
and `style`.

```json
{
  "$type": "textBox",
  "key": "prefix",
  "label": "Prefix",
  "placeholder": "e.g. WIP_",
  "defaultValue": "WIP_",
  "requiredIf": { "$type": "constant", "value": true }
}
```

Containers nest their children:

```json
{
  "$type": "groupBox",
  "header": "Advanced",
  "visibleIf": { "$type": "comparison", "key": "mode", "operator": "equals", "operand": "manual" },
  "children": [ { "$type": "checkBox", "key": "verbose", "content": "Verbose logging" } ]
}
```

### Discriminators

| Inputs | Display | Containers |
| --- | --- | --- |
| `textBox` `password` `numeric` `integer` `slider` | `label` `markdown` `image` | `vStack` `hStack` `grid` |
| `dropdown` `radioGroup` `checkBox` `toggle` | `separator` `spacer` | `groupBox` `tabs` `tabPage` |
| `listSelection` `treeSelection` | `progress` `button` | `expander` `card` `scroll` |
| `datePicker` `colorPicker` `filePicker` `folderPicker` | | `dock` `splitView` |

## Conditions

```json
{ "$type": "comparison", "key": "mode", "operator": "equals", "operand": "manual" }
{ "$type": "logical", "operator": "and", "terms": [ ... ] }
{ "$type": "constant", "value": true }
```

Operators: `equals` `notEquals` `greaterThan` `greaterThanOrEqual` `lessThan` `lessThanOrEqual`
`contains` `notContains` `startsWith` `endsWith` `isEmpty` `isNotEmpty` `isChecked` `isNotChecked`
`in` `notIn` `matches`.

## Computed values

```json
{
  "$type": "arithmetic",
  "operator": "multiply",
  "left":  { "$type": "field", "key": "quantity" },
  "right": { "$type": "field", "key": "unitPrice" }
}
```

Kinds: `constant` `field` `format` `sum` `arithmetic` `lookup` `conditional`.

## Rules

```json
{ "$type": "range", "minimum": 1, "maximum": 200 }
{ "$type": "regex", "pattern": "^[A-Z]{3}-[0-9]{4}$", "message": "Use the form ABC-1234." }
```

Kinds: `required` `range` `regex` `length` `fileExists` `comparison` `custom`.

## How values are written

Loosely-typed values — defaults, condition operands, option values — need care, because JSON has
fewer types than a Dynamo port does.

**Numbers keep their type.** A whole `double` is written as `3.0` rather than `3`, so a slider
default of `3.0` does not come back as the integer `3` and quietly change the form.

**Dates and colours are tagged**, because JSON has neither:

```json
"defaultValue": { "$date": "2026-03-14T09:30:00.0000000" }
"defaultValue": { "$color": "#3366CC" }
```

**Objects JSON cannot carry degrade to their text**, and say so:

```json
"value": { "$opaque": "Wall <312840>" }
```

This is lossy and deliberately visible. A form whose dropdown options are live Revit elements is
not a portable document — the elements do not exist in another model. Saving one produces a file
that loads, but whose options are now strings. If you need a portable form that picks Revit
elements, build the options in the graph and save the form without them.

**Custom predicate rules do not survive.** `CustomPredicateRule` holds a delegate; it round-trips
as a rule that always passes. Keep those for in-process use.

## Loading a form a graph did not build

```
Form.FromJson(json)  ──►  Form.ShowDefinition(form, trigger)
```

`Result.HasKey` is worth using here — a form loaded from a file may not have the fields your graph
expects. `Form.Check` reports authoring problems (conditions naming fields that do not exist,
duplicate keys, loops between computed values) without showing anything.

## Hand-editing

The JSON is meant to be readable and is safe to edit by hand: enums are written as names, accented
text is not escaped, and comments and trailing commas are tolerated by the reader.

The preview harness reloads and reshows a form whenever the file is saved, which makes hand-editing
a genuinely fast way to work:

```bash
dotnet run --project tools/Interlude.Preview
# Open JSON…, tick "Reload and reshow when the file changes", then edit in your editor
```
