# Sample graphs

Dynamo graphs you can download and open. There are two places one can live, and which you want
depends on who is meant to find it.

| | |
| --- | --- |
| **[`docs/nodes/`](../nodes/README.md)** | A graph named after a node. Dynamo's own help panel offers it to open, and it ships inside the package. |
| **`docs/samples/`** | A graph that belongs to a documentation page rather than to a node. Downloaded from this site only. |

## Graphs on this site

| Graph | Dynamo | What it shows |
| --- | --- | --- |
| [A first form](simple-form.md) | 4.2 | A text box and a toggle, shown and read back. |

!!! note "Not the same as `samples/` in the repository root"

    [`samples/`](../../samples/) holds **forms** as JSON — the output of `Form.ToJson`, loaded
    with `Form.FromJson` or opened in the preview harness. Those are generated from the preview
    harness's gallery and verified against it on every build. The graphs here are `.dyn` files,
    opened in Dynamo, and are hand-authored.

---

## Adding a graph to a node's help

This is the one to reach for when a graph demonstrates a *node*. It predates this site and is
described in full in [`docs/nodes/README.md`](../nodes/README.md); the short version:

1. Save the graph as `<Namespace>.<Class>.<Method>.dyn` — `Interlude.Form.Show.dyn` — into
   `docs/nodes/`, beside the node's Markdown page.
2. Optionally add `<node>_img.png` beside it for the screenshot.
3. Regenerate the node help. The `## Example File` section appears on its own:

    ```powershell
    dotnet build tools/Interlude.Preview -c Release
    ./tools/Interlude.Preview/bin/Release/net10.0-windows/Interlude.Preview.exe --docs docs/nodes
    ```

The graph and the image are hand-placed; only the Markdown around them is generated. One graph
usually covers several nodes, so it is copied once per node name — that is what Dynamo's browser
matches on.

What this site adds: on the built pages, the filename in that section becomes a **download
button**. Nothing in the generated Markdown says so, and nothing needs to — the site rewrites it
at build time when the file is really there. See `scripts/mkdocs_hooks.py`.

!!! warning "`docs/nodes/*.md` is generated — never edit it by hand"

    The Markdown comes from the `///` comments on the node, and the next build overwrites any
    edit. CI fails the pull request that contains one. To change what a node's help says, change
    the node's XML documentation comment in `src/Interlude/Nodes/`.

## Adding a graph to a documentation page

For a graph that illustrates a *pattern* rather than a node — a wizard, a gate, a takeoff — put
it here instead. Three steps:

**1. Save the `.dyn` into `docs/samples/`.** Name it after the page that will explain it, in
lower case with hyphens: `simple-form.dyn` beside `simple-form.md`. Keep it small enough to read
at one screen's zoom.

**2. Write the page,** and close it with the download. Use the `dyn-download` class so the link
renders as a button rather than as prose:

```markdown
[Download simple-form.dyn](simple-form.dyn){ .dyn-download download }
```

`attr_list` turns the braces into attributes. `download` is the HTML attribute that tells the
browser to save the file rather than try to display it, which matters because a `.dyn` is JSON
underneath and some browsers will happily render it as text instead.

**3. Add it to the nav** in `mkdocs.yml`, under `Sample graphs`, and to the table above.

Any page can link a graph, wherever it lives — adjust the relative path for where the page sits.
From `docs/recipes.md`, a graph in this folder is `samples/wizard.dyn`; from a page in
`docs/nodes/`, it is `../samples/wizard.dyn`.

## How the file reaches the site

MkDocs copies every non-Markdown file under `docs/` into the built site untouched, so a `.dyn`
dropped into either folder is published at the matching URL with no configuration. It is copied
byte for byte — `shutil.copy`, not a text transform — so nothing rewrites line endings or
re-encodes the JSON on the way through.

The repository's [`.gitattributes`](../../.gitattributes) marks `*.dyn` and `*.dyf` as `binary`,
which stops Git normalising line endings inside them on checkout. That matters more than it
sounds: a graph is JSON with embedded code strings, and a checkout that rewrites CRLF to LF
inside one changes a file Dynamo wrote and expects to read back unchanged.

!!! warning "Say which Dynamo saved it"

    A graph saved in Dynamo 4.x will not open in 3.0. Name the version at the top of the page,
    and prefer the oldest that can express the pattern — 3.0 is the floor Interlude supports.
    [Installing](../installing.md) has the Dynamo-to-Revit mapping.
