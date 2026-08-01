---
name: interlude-form
description: Write an Interlude form for Dynamo from a description of what it should ask. Produces a form.json that the Form.FromJson node loads. Use when someone wants a dialog, a settings window, or a page of inputs in front of a Dynamo or Revit graph, when they mention Interlude or Data-Shapes, or when they are hand-editing a form file and want it checked.
---

# Writing an Interlude form

Interlude is a forms package for Dynamo. A form is a document: `Form.FromJson` reads one,
`Form.ShowDefinition` shows it, and the answers come back in a dictionary keyed by field.

Your job is to turn a description of a form into that document, check it, and hand it over. You
are writing **data, not a graph** — no nodes, no wires.

## The loop

1. **Understand what is being asked.** See *Deciding what to ask* below. Do not interview
   someone who has already told you what they want; a description with five fields in it is a
   specification, not an opening offer.
2. **Write `form.json`.** Structure and property names come from
   [reference/schema.md](reference/schema.md), which is generated from the Interlude assembly and
   is the authority. Judgement comes from [reference/authoring.md](reference/authoring.md).
3. **Check it** with the bundled validator, and fix whatever it reports. It lives in this skill's
   own folder, not in the working directory:
   ```
   ~/.claude/skills/interlude-form/bin/interlude-check.exe form.json
   ```
   If the skill was installed into a project rather than your home folder, it is
   `.claude/skills/interlude-form/bin/interlude-check.exe` instead. Use whichever exists.

   It reports what the reader will refuse, plus conditions naming fields that do not exist and
   computed values that depend on each other in a loop. A form you have not checked is not
   finished. If the validator will not run, say so plainly rather than implying the form was
   verified.
4. **Show the form's shape in your reply** — the fields, in order, with the key each answer will
   arrive under. Those keys are what the rest of the graph is written against, so they are the
   part worth reading. Do not paste the whole JSON back.
5. **Say how to use it**, once:
   ```
   File.FromPath → File.ReadText → Form.FromJson → Form.ShowDefinition
   ```
   Then `Result.GetString(values, "prefix")` and friends read the answers.

## Deciding what to ask

The description names the fields most of the time. When it does not, the gaps worth one round of
questions are the ones that change the document:

- **Answers that drive the graph.** "Rename views" needs to know whether the answer is a prefix,
  a suffix or a find-and-replace. Guessing produces a form for a different job.
- **Which choices are fixed.** A drop-down needs its options. If they come from the model — levels,
  view templates, families — they cannot be in the file, and the form should ask for them a
  different way. See *Options that are not in the file* in the authoring notes.
- **Whether a field is optional.** It changes `requiredIf` and it changes what a cancelled run
  returns.

Everything else — widths, ordering, whether something is a card or a group box — is yours to
decide. Choose, mention the choice in a clause, and move on.

## The rules that catch people

These are the ones that produce a form that loads and then behaves wrongly, which is worse than
one that does not load. The rest is in [reference/authoring.md](reference/authoring.md).

- **Give every field a real `key`.** Without one the key is a slug of the label, so renaming
  "Wall Type" to "Wall type" silently renames the answer from `wall_type` and the graph reading it
  starts getting nothing. Keys are the contract; labels are prose.
- **Two fields with the same key do not collide, they rename.** The second becomes `prefix_2`
  and nothing warns you. Check your keys are distinct.
- **`schemaVersion` is `1`** and goes at the top. A file without it is refused.
- **Whole numbers need a decimal point.** Write `3.0`, not `3`, anywhere a `number` is wanted —
  a slider default of `3` comes back as an integer and changes the control's behaviour.
- **Dates and colours are tagged**: `{ "$date": "2026-03-14T09:30:00.0000000" }` and
  `{ "$color": "#3366CC" }`. JSON has neither type.
- **Name a theme preset, do not write a palette.** `"theme": { "preset": "neubrutalist" }` — the
  eighteen colours travel as that name. Write the colours out only if the user actually chose
  different ones.
- **A cancelled form returns every field's default, not nulls**, with `wasSubmitted` false. So
  defaults should be values the graph can survive, and anything downstream should check
  `wasSubmitted` first. Mention this when the form has destructive consequences.

## Checking a form someone else wrote

Same validator, same path as above:

```
~/.claude/skills/interlude-form/bin/interlude-check.exe path/to/form.json
```

Report what it says. Then read the file for the things a validator cannot see: keys derived from
labels, whole numbers written without a decimal point, a palette written out where a preset would
do, required fields with no default.

## What is in this folder

| | |
| --- | --- |
| `reference/schema.md` | Every element, property, condition, computed value, rule and enum. Generated from the assembly — it cannot be out of date, and it is the authority when it disagrees with anything here. |
| `reference/authoring.md` | How to choose between controls, and the judgement the schema cannot express. |
| `samples/` | Nine worked forms, the same ones the Interlude test suite validates on every build. Read one before writing your first. |
| `bin/interlude-check.exe` | The validator. |
