# Node help

One Markdown file per node, in the format Dynamo's documentation browser reads. When a user
selects an Interlude node and opens **Help**, this is what appears in the panel beside the graph.

**These files are generated. Do not edit them by hand** — the next build overwrites them, and CI
fails the pull request that contains the edit.

## Where they come from

Everything is read out of the shipped assembly: the signature, port names, types and defaults by
reflection, and the prose from `Interlude.xml`, the documentation file the compiler emits beside
the DLL. That is the same file Dynamo reads for its port tooltips, so the help panel and the
tooltip cannot say different things.

The consequence worth stating plainly: **to change what a node's help says, change the `///`
comment on the node.** A node that gains a port gains a documented port, and a summary that is
reworded ships reworded. Help that disagrees with the node it documents is worse than no help at
all, because the reader believes it.

```bash
dotnet build tools/Interlude.Preview -c Release
./tools/Interlude.Preview/bin/Release/net10.0-windows/Interlude.Preview.exe --docs docs/nodes
```

Two things hold this in place. [`ApiSurfaceTests`](../../tests/Interlude.Tests/ApiSurfaceTests.cs)
fails when a node has no help file, which catches the forgotten regeneration after adding one; CI
regenerates the whole folder and fails on any difference, which catches the reverse.

## Where they ship

[`scripts/pack.ps1`](../../scripts/pack.ps1) copies this folder into every package as `doc/`,
beside `bin/`. Dynamo looks for that exact folder name in a package's root and matches each file
to a node by its file name.

```
Interlude/
  bin/    Interlude.dll, Interlude.xml, Interlude_DynamoCustomization.xml
  doc/    Interlude.Input.TextBox.md, ...
  extra/  samples, licences
```

## Naming

`<Namespace>.<Class>.<Method>.md` — so `Interlude.Input.TextBox.md`. Overloaded nodes carry their
parameter names, `Interlude.Theme.Create(mode, accent).md`, which is the convention Dynamo uses
for its own overloads and the one its browser resolves.

## Format

Dynamo's own fallback docs, followed exactly: an `## In Depth` heading, the signature in backticks,
prose, a bullet per input, then the outputs. An `## Example File` section with a screenshot is
emitted only when an image named `<node>_img.png` is sitting in this folder — an image reference
to a file that is not there renders as a broken image in the panel, which reads as a packaging
fault. There are no images yet.

## Prose, not tables

Dynamo's 1,039 built-in help files contain no Markdown tables at all, and these match that. The
tabular reference — every node and port in one page, sortable by eye — is
[docs/node-reference.md](../node-reference.md), which is written for reading in a browser rather
than in a 300-pixel panel.
