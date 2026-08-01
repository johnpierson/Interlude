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
prose, a bullet per input, then the outputs. Each page ends with its family's shared rules, because
a reader who arrived at one node from the library has not read the class summary anywhere else.

## Example graphs

A node with a `<node>.dyn` beside its page gets an `## Example File` section, and Dynamo's
documentation browser offers the graph to open. A `<node>_img.png` is shown under it.

Both files are hand-placed, not generated. The section appears only once one of them exists: an
image reference to a file that is not there renders as a broken image in the panel, which reads as
a packaging fault rather than as a page nobody has illustrated yet.

One graph usually demonstrates several nodes at once, and its screenshot is the same picture on
every page. Copying a 190 KB image once per node would put it in the package several times over,
and again for each Dynamo version, so `SharedExampleImages` in
[`NodeDocs.cs`](../../tools/Interlude.Preview/NodeDocs.cs) points those pages at one copy. The
*graph* is still duplicated per node — it is small, and a per-node name is what the browser needs
to find it.

Currently `SampleInterludeForm.dyn` covers `Form.Show`, `Input.TextBox` and `Input.Toggle`.

## No generated images

Everything in this folder regenerates byte-for-byte, which is what lets CI check it for drift.
Rendering a picture on each run would break that: text rendering varies with the machine, so the
image would differ between a developer's build and CI's and fail every pull request. Screenshots
are checked in and left alone.

## Prose, not tables

## Prose, not tables

Dynamo's 1,039 built-in help files contain no Markdown tables at all, and these match that. The
tabular reference — every node and port in one page, sortable by eye — is
[docs/node-reference.md](../node-reference.md), which is written for reading in a browser rather
than in a 300-pixel panel.
