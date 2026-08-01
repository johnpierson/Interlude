# Third-party notices

Interlude ships one assembly with no runtime package dependencies. It does embed one third-party
work, listed below.

---

## Space Grotesk

**Used for:** Interlude's default interface font, embedded inside `Interlude.dll`.

**Copyright:** Copyright 2020 The Space Grotesk Project Authors
(<https://github.com/floriankarsten/space-grotesk>)

**Licence:** SIL Open Font License, Version 1.1. No reserved font name.

The full licence text ships with every Interlude package at
`extra/SpaceGrotesk-OFL.txt`, and lives in this repository at
[`src/Interlude/Fonts/SpaceGrotesk-OFL.txt`](src/Interlude/Fonts/SpaceGrotesk-OFL.txt).

The OFL permits embedding: *"Permission is hereby granted, free of charge, to any person obtaining
a copy of the Font Software, to use, study, copy, merge, embed, modify, redistribute, and sell
modified and unmodified copies of the Font Software."* The font is embedded unmodified.

Four static faces are embedded — Light, Regular, Medium and Bold — taken from the upstream project
rather than from Google Fonts, which distributes Space Grotesk only as a variable font. WPF has no
support for variable font axes: it renders the default instance and synthesises everything else,
which would give a smeared, algorithmic bold for every heading and button the theme sets in heavy
type.

### Why it is embedded rather than referenced

A font named but not installed renders as whatever the host machine happens to substitute. For a
package distributed to other people's Revit installations, that means "almost anything." Embedding
it as a WPF resource puts the font inside `Interlude.dll`, so every form looks the same everywhere
and the package still ships exactly one file.

### Replacing it

Name any font on the `fontFamily` port of `Theme.Create`:

```
Theme.Create(fontFamily: "Segoe UI")
```

Interlude falls back to Segoe UI Variable Text, Segoe UI and Tahoma, in that order, if a named
font is unavailable.

---

## Not bundled

For the avoidance of doubt, Interlude does **not** ship, vendor or depend on any other third-party
code at run time. `DynamoVisualProgramming.ZeroTouchLibrary` is referenced at compile time only,
with `ExcludeAssets="runtime"`, for its attributes; Dynamo supplies its own copies at load time.
The test project additionally references `DynamoVisualProgramming.Core` so it can run Dynamo's real
zero-touch importer, and is never distributed.
