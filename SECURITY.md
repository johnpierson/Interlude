# Security

## Reporting a vulnerability

Please report security issues privately through
[GitHub's advisory form](https://github.com/johntpierson/Interlude/security/advisories/new)
rather than as a public issue.

Include what an attacker could achieve, how to reproduce it, and the Interlude and Dynamo versions
involved. You can expect an acknowledgement within a few days.

## What Interlude does and does not do

Worth knowing when judging whether something is a vulnerability.

**It does not:**

- Make network requests of any kind.
- Read or write files, except the paths a user chooses in a file or folder field.
- Write to the registry, other than reading the Windows light/dark theme setting.
- Persist anything outside its own package folder. Remembered answers live in memory and are gone
  when Dynamo closes.
- Load code. There is no plugin loading and no reflection over user-supplied types.

**It does:**

- Open web links from `Layout.LinkButton` and from Markdown links, **restricted to `http` and
  `https`**. A form definition can arrive from a downloaded package, and a link is not a licence
  to launch an arbitrary executable.
- Evaluate regular expressions supplied by a form author, in `Rule.Regex` and
  `Condition.Matches`. These run with a one-second timeout so that a pathological pattern cannot
  wedge the UI thread.
- Deserialize form definitions from JSON. The reader is `System.Text.Json` restricted to a closed
  set of known types — a form file cannot name a type to instantiate.

## Passwords

`Input.Password` masks the field on screen. **The answer is returned as plain text** in the
results dictionary, like every other answer, and Dynamo will show it in a node preview or a watch
node.

It exists so a user is not typing a credential in full view of a room. It is not secure storage,
and Interlude has no way to make it so — the value has to reach your graph. Do not put a returned
password anywhere it will be written to disk, and be aware that a saved graph's cached node values
can include it.

## Supported versions

Security fixes go to the latest release. Given the package's size and dependency footprint,
back-porting is unlikely to be necessary; if it is, say so in the report.
