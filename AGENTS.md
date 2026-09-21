# Repository Guidelines

## Project Structure & Module Organization

This is a Unity 6.5 (`6000.5.10f1`) URP 2D procedural-generation project. Core project content lives under `Assets/00_Main`:

- `00_Scenes/` contains playable scenes, primarily `PGDemoScene.unity`.
- `01_Scripts/` contains C# code. Keep feature code grouped by domain, such as `Tile/Break`, `Tile/UI`, and `SO`.
- `02_Assets/`, `03_SO/`, and `10_Tilemaps/` contain art assets, ScriptableObject data, and Tilemap resources.
- `Assets/Plugins/` and `Assets/Samples/` are third-party or sample content; avoid editing them unless the task explicitly requires it.

Keep every Unity asset's `.meta` file with its asset. Do not manually change generated `Library/`, `Temp/`, or `Logs/` content.

## Build, Test, and Development Commands

Open the repository through Unity Hub using Unity `6000.5.10f1`; this refreshes generated solution files and imports assets.

```powershell
dotnet build Assembly-CSharp.csproj --no-restore -v:minimal
```

Use this for a fast C# compile check after Unity has refreshed the project. Run tests through Unity's Test Runner. For CI or headless execution, use Unity's `-runTests -testPlatform EditMode` arguments and write results under `Logs/`.

## Coding Style & Naming Conventions

Use four-space indentation, braces on their own lines, and one public type per file. Use PascalCase for types, methods, properties, and ScriptableObject assets; use camelCase for private serialized fields (for example, `progressFill`). Group fields with `[Header]` where it improves Inspector readability.

Keep MonoBehaviours focused: event coordinators should not directly own rendering details. Put UGUI concerns in view components and inject references through serialized fields. Use `Try`/null-safe handling for optional presentation references, but log clear warnings for required gameplay references.

## Testing Guidelines

The Unity Test Framework is installed, but this repository currently has no dedicated test assembly. Add new EditMode tests under `Assets/Tests/EditMode/` and create an `.asmdef` that references the production assembly. Name tests as `Method_Condition_ExpectedResult`. For UI, Tilemap, or scene changes, verify Play Mode behavior and attach a screenshot or short recording to the PR.

## Commit & Pull Request Guidelines

Recent history uses scoped conventional-style subjects such as `feat : TMP` and `refactor : Managers`. Follow that pattern: `feat : add loading progress view` or `fix : clamp tile generation progress`.

Keep commits focused. PRs should describe behavior changes, list validation performed, link related issues when available, and include scene/UI evidence for visual changes. Include all changed `.meta` files and avoid unrelated serialized scene edits.
