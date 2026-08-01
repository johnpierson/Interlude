# The Interlude form skill

Describe a form; get one. This is a [Claude Code](https://claude.com/claude-code) skill that
turns "ask for a prefix, a check box for sheets, and hide the folder picker unless it's ticked"
into a `form.json` the `Form.FromJson` node loads — checked before you see it.

It is a **separate download** from the Interlude package. Nothing here goes into your Dynamo
packages folder, and you do not need it to use Interlude.

## Installing

Unzip it into your skills folder and restart Claude Code.

```powershell
Expand-Archive Interlude-skill-1.0.0.zip -DestinationPath "$env:USERPROFILE\.claude\skills"
```

That gives you `~/.claude/skills/interlude-form/`, available in every project. To scope it to one
repository instead, unzip into that repository's `.claude/skills/` folder and commit it.

Check it is there by asking for a form:

> Make me an Interlude form that asks for a view prefix and whether to include sheets.

## The validator

`bin/interlude-check.exe` reads form JSON and reports what is wrong with it — anything the reader
would refuse, plus conditions naming fields that do not exist and computed values that depend on
each other in a loop. The skill runs it on everything it writes; it is also useful on its own:

```powershell
.\bin\interlude-check.exe my-form.json
```

It exits non-zero when anything is wrong, so it works in a build. Point it at a folder to check
every form in it.

It needs the **.NET Desktop Runtime**, version 8 or newer — which any machine running Dynamo 3.0
or later already has. It is not signed, so Windows may want convincing the first time.

## What is in here

| | |
| --- | --- |
| `SKILL.md` | What Claude reads. |
| `reference/schema.md` | Every element, property, condition, computed value, rule and enum, generated from the Interlude assembly. |
| `reference/authoring.md` | Choosing between controls, and the judgement the schema cannot express. |
| `samples/` | Nine worked forms, the same ones Interlude's test suite validates on every build. |
| `bin/` | The validator. |

## Seeing the form

The skill writes a file; it cannot show you the window. To look at one before wiring it into a
graph, use the preview harness from the
[Interlude repository](https://github.com/johnpierson/Interlude), which reloads and reshows a form
whenever the file is saved:

```bash
dotnet run --project tools/Interlude.Preview
```

## Licence

BSD 3-Clause, the same as Interlude. See `LICENSE.txt`.
