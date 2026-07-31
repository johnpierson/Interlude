# Installing

Interlude ships one assembly per Dynamo version. Pick the one matching your Dynamo.

| Download | Dynamo | Revit |
| --- | --- | --- |
| `Interlude-<version>-dynamo3.0.zip` | 3.0 | 2025 |
| `Interlude-<version>-dynamo3.6.zip` | 3.6 | 2026 |
| `Interlude-<version>-dynamo4.0.zip` | 4.0 | 2027 |

Not sure which you have? In Dynamo, **Help → About**.

Dynamo 2.x and Revit 2024 and earlier are out of scope: they run on .NET Framework, and Interlude
is .NET 8 and later.

## Install

1. Download the archive for your Dynamo version from
   [Releases](https://github.com/johntpierson/Interlude/releases).
2. **Unblock it before unzipping.** Right-click the `.zip` → Properties → tick **Unblock** → OK.
   Windows marks downloaded files, and the mark propagates to the DLL inside; Dynamo will refuse
   to load a blocked assembly, usually without saying why.
3. Unzip into your Dynamo packages folder, so you end up with an `Interlude` folder there:

   ```
   %AppData%\Dynamo\Dynamo Revit\<version>\packages\Interlude\
     pkg.json
     bin\Interlude.dll
     bin\Interlude.xml
     bin\Interlude_DynamoCustomization.xml
     extra\samples\
   ```

   For Dynamo Sandbox the path is `%AppData%\Dynamo\Dynamo Core\<version>\packages\`.

4. Restart Dynamo. **Interlude** appears in the library.

You can also point Dynamo at the folder directly: **Settings → Manage Node and Package Paths**.

## Check it worked

Place **Interlude → Form → Show**, wire an **Input → TextBox** into its `elements` port, and run
the graph in **Manual** mode. A dialog should appear.

If the category is missing, see below.

## Troubleshooting

**The Interlude category does not appear.**
Almost always the blocked-file problem. Right-click `bin\Interlude.dll` → Properties → tick
**Unblock**, then restart Dynamo. If it persists, check that all three files are in `bin\` — the
`.xml` files are not optional decoration, Dynamo reads them for port names and for the library
category.

**Nodes appear under strange categories.**
`Interlude_DynamoCustomization.xml` is missing from `bin\`.

**Port names show as `var` with no tooltips.**
`Interlude.xml` is missing from `bin\`.

**The dialog appears more than once.**
Dynamo is in Automatic run mode and something upstream keeps changing. Switch to Manual, or gate
the form with `Form.Show`'s `trigger` input. See
[re-execution](../README.md#re-execution).

**"This form cannot be shown because this Dynamo session has no user interface."**
The graph is running with no UI — a command-line run, a scheduled job, Generative Design. Set
`headlessUseDefaults` to `true` on `Form.Show` to return each field's default instead of stopping.

**The dialog opens behind Revit.**
Please [open an issue](https://github.com/johntpierson/Interlude/issues) with your Revit version.
Interlude owns its window to the host's active window, and a case where that fails is a bug worth
knowing about.

## Uninstalling

Delete the `Interlude` folder from your packages directory and restart Dynamo. Nothing is written
outside it, and no settings are stored anywhere else.

## Building it yourself

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download), which compiles every target in
the matrix.

```powershell
git clone https://github.com/johntpierson/Interlude.git
cd Interlude
./scripts/build-all.ps1 -Pack
```

The packages appear under `dist/`, laid out exactly as above.
