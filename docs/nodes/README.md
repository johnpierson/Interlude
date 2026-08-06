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

A `<node>_form.png` is shown under that, captioned "The form it builds" — the graph picture answers
what to wire, and the reader deciding whether this is the node they want is asking what it looks
like. The section appears only once one of these exists: an image reference to a file that is not
there renders as a broken image in the panel, which reads as a packaging fault rather than as a
page nobody has illustrated yet.

### Only one of the two pictures ships

The canvas pictures are ~200 KB each, and 114 of them was most of the package. So the pages link
them from `raw.githubusercontent.com` on `main`, [`pack.ps1`](../../scripts/pack.ps1) leaves them
out of `doc/`, and the package is 7 MB rather than 30 MB.

The form pictures are a tenth of that in total and still ship, which is the half of the trade worth
keeping: Dynamo renders this panel in an embedded browser, so a machine with no route to GitHub —
and plenty of practices are locked down that way — gets no canvas picture. It still gets the page,
the graph, and a picture of the form the node builds, which is what a reader deciding between two
nodes is looking at anyway.

The link is to a branch rather than to a release tag on purpose. A tag would pin each release's
help to the pictures it shipped with, which is tidier, but it 404s for every commit made before
that tag is pushed — and the rule for the local case above is the same one: never point at what is
not there. **The two ends have to agree**: `NodeDocs.CanvasImages` writes the link and `pack.ps1`
decides what to copy, and a page pointing at a picture that is in neither place is worse than
either choice.

Every node gets its own graph rather than sharing one, because a shared graph answers the question
"what does this package do" and a reader on a node's page is asking "what does *this node* do".

### The graphs are generated

[`examples.spec.json`](examples.spec.json) holds one entry per node — the function signature, the
literal arguments, the key to read back, and the prose that becomes the group titles — and
[`scripts/make-node-examples.mjs`](../../scripts/make-node-examples.mjs) writes the `.dyn` files
from it. **Edit the spec, not the graphs.**

```bash
node scripts/make-node-examples.mjs
```

Every graph has the same four groups: describe the field, show it, read the answer back, and keep
it as a document. `trigger` is wired to a `false` boolean, so a graph runs to completion without
putting a dialog on screen — the reader sets it to true to see the form.

A spec entry names its node, the literal arguments, and optionally `children` — the nodes built
upstream of it, which nest to any depth. An argument of `$children` takes all of them as a list,
`$child0` one of them, and `["$child1"]` a chosen few.

Nodes that do not return a form element — a condition, a rule, a computed value, a tree item —
carry a `graph` instead. Its root is whatever *consumes* the node, and the node itself sits inside
it as a child. `Condition.IsChecked` is the shape of it: the graph is a column holding a tick box
and a field that `Behavior.VisibleIf` hides until the box is ticked. Putting the driving field in
the form alongside the driven one is the point — a condition nobody can see working is not an
example of anything.

Three families attach to the *tail* of the graph rather than to the form's contents, and each has
a field of its own:

- **`theme`** and **`options`** are branches built exactly like the fields are, wired to the
  Form.Show port of the same name and laid out beneath rather than beside. The harness passes both
  to `Form.Create`, so a Theme node's picture is drawn in the theme it is about — a page for
  `Theme.Mono` showing the default look would be illustrating nothing.
- **`tail`** replaces the end of the graph. `"document"` adds the round trip — `Form.FromJson`,
  `Form.WithOptions`, `Form.Check` and `Form.ShowDefinition` — which four Form nodes share, because
  none of them means much on its own. `"forget"` adds `Form.Forget` wired to the same `formId` the
  form uses; wired to an id nothing uses it would run green and clear nothing.
- The **Result** nodes need no field at all. The template already ends in a reader wired to
  `Form.Show`'s `form` output, so a Result page is an ordinary spec whose `getter` happens to be
  the node the page is about.

Whichever group holds the page's node is the one that gets the spec's `note`. A note filed under
the wrong heading is worse than a heading with no note, because the reader takes it as describing
what it sits above.

The generator writes the `View` section itself rather than saving through Dynamo. That is not
incidental: **saving a workspace over the Dynamo MCP drops `View` entirely**, so a graph saved that
way loses every node position, group and note and opens as an unpositioned pile.

### The pictures are not

Two commands, both run by hand and their output checked in:

```bash
node scripts/make-node-examples.mjs
./tools/Interlude.Preview/bin/Release/net10.0-windows/Interlude.Preview.exe --forms docs/nodes
```

`--forms` reads the same spec and *calls each node* — resolving the method from its signature and
handing it the same literal arguments the graph gets — then renders what comes back with the real
WPF renderer. The picture on the page is therefore drawn by the code that will draw the form at run
time, not by a second description of the node that can quietly fall out of step with the first.

This was originally done the long way round: run each graph in Dynamo, read the definition off a
`Form.ToJson` node, and render that. Both routes produce byte-identical PNGs — which is the
argument for the short one, since it needs no running Dynamo and does all 114 in one command.

The canvas pictures are the part that still needs Dynamo: open the graph and export the workspace
image. Both kinds stay checked in and are left alone by the doc build, for the reason below.

## No generated images

Everything in this folder regenerates byte-for-byte, which is what lets CI check it for drift.
Rendering a picture on each run would break that: text rendering varies with the machine, so the
image would differ between a developer's build and CI's and fail every pull request. Screenshots
are checked in and left alone.

## Prose, not tables

Dynamo's 1,039 built-in help files contain no Markdown tables at all, and these match that. The
tabular reference — every node and port in one page, sortable by eye — is
[docs/node-reference.md](../node-reference.md), which is written for reading in a browser rather
than in a 300-pixel panel.
