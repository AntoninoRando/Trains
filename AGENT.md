# AGENT.md — AI Agent Guide for the Trains Project

Guide for AI assistants working on this codebase. Read this before making changes.

## Project Overview

**Trains** is a 2D game built with **Godot 4.6 (C#, .NET 8, Forward Plus)**. The player controls up to 5 trains running on paths; pressing the assigned key (1–5) or clicking near a train makes it "sprint". The first train to reach the end of its path wins the stage and carries over to the next one. Trains colliding ("bump") means defeat.

- Engine: Godot 4.6.2 (`Godot.NET.Sdk/4.6.2`), C# only (no GDScript)
- Solution: `Trains.sln` / `Trains.csproj` (assembly name `Trains`)
- Main scene: `Scenes/Main.tscn`; menu: `Scenes/MainMenu.tscn`

## Build & Run

```bash
dotnet build Trains.sln          # compile C#
godot --path . --headless --quit # reimport assets / verify project loads
godot --path .                   # run the game (requires Godot 4.6 with .NET support)
```

There are no automated tests. Verify changes by building and, when possible, running the project.

## Directory Layout

```
Scenes/            Top-level scenes (Main, MainMenu, Stage, OfUI/Pedal)
Scripts/           All C# code, grouped by domain
  Match/           Match (logic) + MatchNode2D (view) — game session, stage cycling, win/lose
  Stage/           Stage (logic) + StageNode2D (view), TrainsSpawner, StageCamera, CompleteAnimation
  Trains/          Train (logic) + TrainNode2D (view), TrainArea (bump detection), AutoMover
  Paths/           Path (logic) + PathNode2D (view), SpeedLayer, EndPathArea (finish line)
  StageElements/   Mine (logic) + MineNode2D + MineScene.tscn — stops trains to "mine"
  Inputs/          ProximityDetection (mouse-hover nearest train, with hysteresis)
  UI/              Pedal (animated key indicator bound to a Path's sprint events)
  MainMenu/        MainMenu (scene navigation)
  GlobalUtilities/ Log (DEBUG-only logging: Log.Info/Warning/Error)
  IMouldable.cs    Model↔View binding mechanism (see below)
Assets/            Art, audio, TrainScene.tscn, TrainsPaths/0001-0002.tscn (path scenes)
.godot/            Generated cache — never edit or commit-review
```

## Architecture: Model / View Separation

The core pattern of this project. Each game concept has a **pure-logic class** (no Godot dependency, testable) and a **Godot node "view"**:

| Model (pure C#) | View (Godot node) |
|---|---|
| `Match`  | `MatchNode2D : Node2D`  |
| `Stage`  | `StageNode2D : Node2D`  |
| `Train`  | `TrainNode2D : Node2D`  |
| `Path`   | `PathNode2D : Node2D`   |
| `Mine`   | `MineNode2D`            |

Rules to follow:

1. **Game logic goes in the model class.** Models expose state via read-only properties and communicate outward via C# `event Action<...>` (e.g. `Stage.Completed`, `Match.Started`, `Path.SprintStarted`).
2. **Views own Godot concerns only**: scene tree, `[Export]` fields, input polling, tweens, instancing `PackedScene`s. The view creates its model (`readonly Stage stage = new();`), registers itself in `_Ready()` with `((IMouldable)model).SetView(this);`, and subscribes to model events.
3. Views are retrieved from models via `((IMouldable)model).GetView<TrainNode2D>()`.

### ⚠ Known limitation of IMouldable

`ModelViews.views` is a **single static dictionary keyed by view *type***, not by model instance. `SetView` overwrites the previous entry, so `GetView<TrainNode2D>()` returns the **last registered** view of that type — fragile when multiple trains/paths exist. Be careful relying on `GetView` for per-instance lookups; if you fix this, key by model instance and update all call sites (`StageNode2D`, `PathNode2D`, `MatchNode2D`, `StageCamera`).

## Key Mechanics

- **Speed system**: `Path.Speed` = `BaseSpeed` transformed by ordered `SpeedLayer`s (id, priority, `Func<double,double>`). Sprint adds layer `"sprint"` (priority 100, ×`SprintMultiplier`); Mine adds layer `"mine"` (priority 200, speed→0). Add new speed effects as layers, not by mutating `BaseSpeed`.
- **Movement**: `PathNode2D._Process` advances `PathFollow.ProgressRatio` by `path.Speed * delta`. Trains are reparented under the path's `PathFollow2D`.
- **Input**: actions defined in `project.godot` — `train_1`…`train_5` (keys 1–5) and `speed_focused_train` (left click / space / tab, sprints the hovered train via `ProximityDetection`). Max paths per stage: `Stage.PATHS_LIMIT = 5`.
- **Win/lose flow**: `EndPathArea.TrainArrived` → `Stage.OnTrainArrived` → `Stage.Completed` → `MatchNode2D` makes `StageCamera.TrackTrain` pan → `TransitionComplete` → `Match.Start()` builds next stage, carrying over the winning train. `TrainArea` bump → `Stage.TriggerBump` → `Match.Lose()` → defeat UI.
- **Spawning**: `TrainsSpawner.StartStage()` enqueues (train scene, path scene, delay) tuples and spawns them in `_Process`.

## Conventions

- One class per file; file name = class name. `.cs.uid` files are Godot-generated companions — keep them next to their `.cs`, never edit them.
- Code regions used as section separators: `#region FIELDS ----`, `EXPORT FIELDS`, `PROPERTIES`, `EVENTS`, `GODOT LIFECYCLE`, etc. Follow this style in existing files.
- Indentation/charset per `.editorconfig` (4 spaces, UTF-8).
- Logging via `Log.Info/Warning/Error` (compiled only in DEBUG), not `GD.Print` directly (some legacy `GD.Print` calls remain).
- Models raise events; views subscribe. Avoid views calling into other views directly — go through models when possible.
- Resource paths use `res://` (note: a few use `res:///` with triple slash; both work, prefer `res://`).

## Gotchas

- `Trains.csproj.old` is a leftover; the active project file is `Trains.csproj`.
- `Scripts/InputListener.cs.uid` and `Scripts/SceneComplete.cs.uid` are orphans (their `.cs` files were deleted). Safe to remove if cleaning up.
- `CompleteAnimation.PartFour()` is intentionally commented out / returns null.
- The main scene is referenced by UID (`uid://ctm1pvtmwc7es`) in `project.godot`; renaming/moving scenes requires Godot to update UIDs — do it through the editor or run a headless import afterwards.
- Don't touch `.godot/` (caches) — regenerate with a headless run if assets change.
