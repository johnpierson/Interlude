# Sample forms

Worked examples of Interlude forms as JSON. Load one with `Form.FromJson` and show it with
`Form.ShowDefinition`, or open it in the preview harness.

| File | What it shows |
| --- | --- |
| `minimal.json` | The smallest useful form. |
| `every-control.json` | One of each input, for checking alignment and spacing. |
| `every-container.json` | Stacks, grids, tabs, cards, splitters. |
| `conditional-form.json` | Fields that appear, enable and become required in response to others. |
| `computed-values.json` | A quantity takeoff whose totals recalculate as you type. |
| `validation.json` | Rules that fire while typing, including one that reads another field. |
| `long-form.json` | Fifty fields, for checking scrolling. |

## These are tested

[`SampleFormTests`](../tests/Interlude.Tests/SampleFormTests.cs) loads every file here on each
build and checks that it parses, round-trips unchanged, produces a session with nothing to warn
about, and renders. Documentation that no longer parses is worse than none.

## Regenerating

They are generated from the preview harness's gallery, which is the source of truth:

```powershell
dotnet build tools/Interlude.Preview
./tools/Interlude.Preview/bin/Debug/net10.0-windows/Interlude.Preview.exe --export samples
```

CI fails if they have drifted from the gallery.

## Working on one

The harness reloads and reshows a form whenever the file is saved, so editing the JSON in one
window and watching the form in another is the fastest way to iterate:

```bash
dotnet run --project tools/Interlude.Preview
# Open JSON…, tick "Reload and reshow when the file changes"
```

The format is described in [docs/form-json.md](../docs/form-json.md).
