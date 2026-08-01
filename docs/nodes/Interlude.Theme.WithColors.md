## In Depth

`Theme.WithColors(theme, background: "", foreground: "", surface: "", border: "", error: "")`

Replaces individual colours in a theme's palette. Every colour left empty keeps the value it already had.

The inputs are:

- `theme` (_ThemeDefinition_) — The theme to adjust.
- `background` (_string_, defaults to `""`) — Window backdrop, as hex.
- `foreground` (_string_, defaults to `""`) — Main text colour, as hex.
- `surface` (_string_, defaults to `""`) — Panels and cards, as hex.
- `border` (_string_, defaults to `""`) — Control outlines, as hex.
- `error` (_string_, defaults to `""`) — Validation colour, as hex.

Returns `theme` — The adjusted theme.

Search terms: `theme`, `palette`, `colors`, `colours`, `brand`, `override`.
