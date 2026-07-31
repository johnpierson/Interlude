## What this changes

<!-- One or two sentences. What is different for someone using Interlude? -->

## Why

<!-- The problem, not the patch. -->

## Checklist

- [ ] `dotnet test Interlude.sln` passes
- [ ] `./scripts/build-all.ps1` passes for all three Dynamo versions

If this touches the node API (anything under `src/Interlude/Nodes/`):

- [ ] The change is **append-only** — no renamed methods, no reordered or retyped parameters,
      no removed `[MultiReturn]` names. Saved graphs bind to node names and parameter positions.
- [ ] `tests/Interlude.Tests/api-surface.txt` is updated and the diff is in this PR
- [ ] Anything being retired is hidden with `[IsVisibleInDynamoLibrary(false)]` rather than deleted

If this adds a control:

- [ ] New element record, new renderer, registered in `ControlRendererRegistry.CreateDefault`
- [ ] `[JsonDerivedType]` added to `FormElement`
- [ ] Added to the preview gallery and `samples/` regenerated

If this touches rendering:

- [ ] Checked in both light and dark themes in the preview harness
- [ ] No writes to `Application.Current.Resources`
