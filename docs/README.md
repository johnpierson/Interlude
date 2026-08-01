# Interlude documentation

Declarative forms for Dynamo. Start with the [README](../README.md) for what Interlude is; this
folder is the detail.

## Using it

| | |
| --- | --- |
| **[Installing](installing.md)** | Which download, where it goes, and what to do when the category does not appear. |
| **[Node reference](node-reference.md)** | Every node and every port, grouped as the library groups them. |
| **[Recipes](recipes.md)** | Worked patterns: gating a dialog, several outcomes, wizards, live totals, unattended runs. |
| **[Coming from Data-Shapes](migrating-from-data-shapes.md)** | Node-by-node mapping, and the behavioural differences — the cancellation contract first. |
| **[The form skill](../skills/interlude-form/)** | An optional separate download: describe a form and have one written, with a command-line checker for form files. |

## Going deeper

| | |
| --- | --- |
| **[Forms as JSON](form-json.md)** | The schema, and what does and does not survive a round trip. |
| **[Architecture](architecture.md)** | Layering, the reactive session, threading, culture, and the reasoning behind each. |
| **[Contributing](../CONTRIBUTING.md)** | Setting up, the rules with teeth, adding a control. |
| **[Security](../SECURITY.md)** | What Interlude does and does not do, and how to report a problem. |
| **[Original brief](original-brief.md)** | The design document this was built from, before the package had a name. Kept for the reasoning. |

## Elsewhere in the repository

- **[`samples/`](../samples/)** — example forms as JSON, validated against the schema on every build.
- **[`tests/Interlude.Tests/api-surface.txt`](../tests/Interlude.Tests/api-surface.txt)** — the
  machine-generated node signatures the test suite enforces.
- **[`tools/Interlude.Preview`](../tools/Interlude.Preview)** — the harness: sample gallery, live
  theme controls, JSON hot reload, offline screenshot rendering.
- **[`tools/Interlude.Check`](../tools/Interlude.Check)** — the form checker, and the one tool that
  ships. It goes out with the skill rather than with the package.
- **[`skills/interlude-form`](../skills/interlude-form)** — the authoring skill, and the generated
  schema reference behind it.
- **[`versions.json`](../versions.json)** — the build matrix.
- **[`CHANGELOG.md`](../CHANGELOG.md)** — what changed, and when.

## If you read one thing

Three behaviours differ from what people usually expect, and each is deliberate:

1. **Cancelling returns every field's default, never nulls.** Check `wasSubmitted` before acting —
   otherwise a cancelled form will cheerfully do the work with defaults.
2. **A hidden field is never required and never validated.** Its value still appears in the
   results.
3. **`trigger: false` skips the dialog** and returns the last answers, which is how a form
   survives a graph that re-executes.
