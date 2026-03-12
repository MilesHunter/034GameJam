## 034GameJam — Agent-Oriented Project Overview

This document is for AI agents (and human maintainers) to quickly understand the structure and core mechanics of this Unity project.

Engine: **Unity 2022.3 (2D)**  
Main scene in build: `Assets/Scenes/TestScene.unity`

High level: this is a 2D physics toy where the player uses **balls** and **sticks** to build a structure. The structure’s connectivity to the initial ball drives production/unlocks.

---

## Core Gameplay Concepts

### Balls

Abstract base: `Assets/Scripts/Balls/Ball.cs`

- Fields:
  - `isFixed`: fixed vs dynamic body.
  - `connectedToInitial`: whether this ball is connected to the initial ball (set via BFS in `GameManager`).
  - `connectionLimit`, `currentConnections`, `connectedSticks`.
- Responsibilities:
  - Ensure it has a `Rigidbody2D` and configure according to `isFixed`.
  - Track connections to `Stick` instances (`AddConnection`, `RemoveConnection`).
  - Visual feedback: brighten/dim based on `currentConnections`.

Concrete ball types:

- `InitialBall` (`Assets/Scripts/Balls/InitialBall.cs`)
  - The root of the network; always treated as connected.
  - Fixed, non-deletable.

- `AllocatableBall` (`Assets/Scripts/Balls/AllocatableBall.cs`)
  - Player-spawned / auto-spawned connection balls.
  - `Delete()` disconnects from all connected sticks, then destroys the GameObject.
  - These are what the **Select** tool is allowed to select & delete.

- `ProductionBall` (`Assets/Scripts/Balls/ProductionBall.cs`)
  - When `connectedToInitial` becomes true, periodically produces:
    - `AddAllocatableBalls(ballOutput)`
    - `AddGenericSticks(stickOutput)`

- `FunctionBall` (`Assets/Scripts/Balls/FunctionBall.cs`)
  - When first connected to initial, calls `GameManager.UnlockStickLength(unlockStickLength)`.
  - Unlocks new stick length options.

### Sticks

Implementation: `Assets/Scripts/Connection/Stick.cs`

- Physical representation:
  - `Rigidbody2D rb` (required), `SpriteRenderer` on the same GameObject.
  - Two end transforms: `endA`, `endB` as children at local positions (0, ±0.5).
  - World length is `transform.localScale.y`.

- Logical endpoints:
  - `Ball endpointA`, `Ball endpointB` store which balls are connected to each end.
  - `GetFreeEnds()` returns the unconnected ends; `FreeEnd` returns the first available.

- Placement / manipulation state:
  - `StartPlacement(Ball anchorBall)` — called by `GameManager.SpawnStick`:
    - Makes rigidbody kinematic, no gravity, set `isBeingPlaced`.
    - One end is anchored to `anchorBall` (see `AttachEndpointToBall`).
    - In `Update`, `UpdatePlacementPose()` orients the stick towards the mouse.
  - `ConfirmPlacementPose()` — stops placement but keeps kinematic; immediately followed by manipulation.
  - `FinishPlacement()` — exits placement, re-enables dynamic physics.
  - Drag/rotate:
    - `BeginDrag`, `BeginRotate`, `BeginRotateFromAnchor`, `EndManipulation` and `UpdateManipulationPose()` handle moving/rotating sticks while in **Stick tool**.
    - `CanDrag`/`CanRotate` act as constraints (e.g. both ends free vs one end attached).

- Auto-connection (magnetic attraction):
  - `FixedUpdate()` (modified):
    - Skips while `isBeingPlaced` or `isManipulating`.
    - Calls `TryAttract(endA, ref endpointA, 0)` and `TryAttract(endB, ref endpointB, 1)`.
  - `TryAttract`:
    - Overlaps a circle around the end within `attractionRadius` on layer "Ball".
    - Picks closest `Ball` that `CanConnect()` and isn’t already endpointA/B.
    - If within `connectionThreshold`, calls `EstablishConnection` → aligns and creates a `HingeJoint2D`.

- Deletion:
  - `Delete()` disconnects both endpoints and destroys the GameObject. Inventory return is handled by callers (e.g. `InteractionManager` → `GameManager.ReturnStick()`).

### Connectivity & auto-cleanup

- `GameManager.OnConnectionChanged()` → `RecalculateConnectivity()`:
  - BFS from `initialBall` over graph of `Ball.connectedSticks`.
  - Calls `OnConnectedToInitialChanged()` on all balls.

- `ConnectionCleaner` (`Assets/Scripts/Systems/ConnectionCleaner.cs`):
  - Periodically (`cleanInterval`) destroys `AllocatableBall` instances that have `currentConnections == 0` and `hasBeenConnected == true`.

---

## Core Orchestration: GameManager

File: `Assets/Scripts/Core/GameManager.cs`

Responsibilities:

1. **Game phase & physics**
   - Enum `GamePhase { Build, Simulate }` and property `Phase`.
   - `EnterBuild()` sets all sticks and non-fixed balls to kinematic & gravityScale 0.
   - `EnterSimulate()` sets them back to dynamic & gravityScale 1.
   - Toggles via Space key in `Update()`.

2. **Pause & restart**
   - `IsGamePaused`, and ESC toggles pause (unless a stick is being placed/manipulated).  
   - `RestartScene()` bound to `R` key.

3. **Resources & inventory**
   - Base counts:
     - `allocatableBallCount`
     - `stickCount` (generic count if Backpack not present)
     - `availableStickLengths` (list of unlocked stick lengths).
   - Interop with optional `Backpack` (see below): when present, per-length stick counts & ball counts are synchronized.
   - Auto-refresh sticks: if `autoRefreshSticks` is true, periodically `AddGenericSticks(1)`.
   - `ReturnStick()` / `ReturnAllocatableBall()` are used when deleting placed items.

4. **Spawning sticks & balls**
   - `SpawnStick(Ball anchorBall, int length)`:
     - Consumes a generic stick (`TryConsumeGenericStick`) and, if Backpack exists, a stick of that length.
     - Creates a new GameObject with:
       - `SpriteRenderer` (rectangle sprite from `MakeRectSprite`)
       - `BoxCollider2D`, `Rigidbody2D`
       - `Stick` component (with `rb` set)
       - Children `EndA`, `EndB` transforms
     - Calls `stick.Initialize(...)` and `stick.StartPlacement(anchorBall)`, then returns the `Stick` in placement mode.

   - `SpawnAllocatableBall(Stick stick)` / `TryInstallBallOnStickFreeEnd(Stick stick)`:
     - Both create `AllocatableBall` instances near a stick free end, consuming `allocatableBallCount`.
     - `TryInstallBallOnStickFreeEnd` also attaches the ball to the stick’s free end.

   - Auto-ball helpers (currently used by debug/editor utilities):
     - `TryAttachStickFreeEndsToExistingConnections`, `AutoSpawnBallOnStickFreeEnd` and static helpers `TryAttachFreeEndToNearbyBall` / `TryAttachFreeEndToNearbyStickEnd`.

5. **Warehouse system**
   - `stickWarehouse` (Dictionary<int length, int count>) for storing sticks of specific lengths.
   - `StoreStick(Stick stick)` stores free (not fully connected) sticks back into warehouse and returns them to inventory.
   - `TryWithdrawStick(int length, Vector2 spawnPos, out Stick stick)` spawns a physics stick in-world using the same pattern as `SpawnStick`, but not in placement mode.
   - `GetWarehouseSnapshot()` exposes current warehouse content for UI.

6. **Procedural sprites**
   - `MakeCircleSprite()` and `MakeRectSprite()` generate simple white textures at runtime for use by sprite renderers (sticks, balls, ground, etc.).

---

## Player Input & Tools: InteractionManager

File: `Assets/Scripts/UI/InteractionManager.cs`

Singleton: `InteractionManager.Instance` (assigned in `Awake`).

### Build tools

Enum `BuildTool { Stick, Ball, Select }`, state fields:

- `currentTool` (default Stick), `currentStickLength` (default 2).
- `SetStickTool(int length)` — activates Stick tool and sets the length (used by `BuildToolbar`).
- `SetBallTool()` — activates Ball tool.
- `SetSelectTool()` — activates Select tool.

### Runtime state

- Placement & manipulation:
  - `stickBeingPlaced` — a `Stick` currently in placement mode.
  - `manipulatingStick`, `isManipulating` — a stick being dragged/rotated.

- Selection:
  - `selectedBall` (`AllocatableBall` only)
  - `selectedStick` (`Stick`)

### Update loop (high level)

In `Update()` (when `GameManager.Phase == Build` and game not paused):

1. **Keyboard shortcuts**
   - `X` → `TryDeleteSelection()` (only when not placing/manipulating):
     - If a ball is selected: return one ball to inventory (`GameManager.ReturnAllocatableBall`) then `AllocatableBall.Delete()`.
     - If a stick is selected: `GameManager.ReturnStick()` then `Stick.Delete()`.

2. **Right mouse button**
   - If right-click hits a `Stick` that is not fully connected:
     - `GameManager.StoreStick(hitStick)` — store the stick back to the warehouse.
   - If right-click hits nothing and `WarehouseUI` exists:
     - Toggles warehouse open/close.

3. **Left mouse button (primary interaction)**
   - Tracks `pendingLeftDown`, `pendingMouseDownWorld`, `pendingStick`, `pendingBall` to distinguish click vs drag.

   - On mouse-down:
     - Records world position, caches any `Stick`/`AllocatableBall` under the cursor.

   - On mouse-up:
     - If `currentTool == Ball` → `TryInstallBallAtPosition(pendingMouseDownWorld)`:
       - Finds nearest free stick end and calls `GameManager.TryInstallBallOnStickFreeEnd`.

     - If `currentTool == Select`:
       - If `pendingStick` → `SetSelection(pendingStick)`.
       - Else if `pendingBall` → `SetSelection(pendingBall)`.
       - Else → `ClearSelection()`.

     - If `currentTool == Stick`:
       - If `pendingStick != null` → start drag or rotate on that stick.
       - Else → find nearest ball via `GameManager.FindNearestBall` and spawn a new stick anchored to it via `SpawnStick(anchor, currentStickLength)`. The resulting stick goes into placement mode (`stickBeingPlaced`).

   - While holding left mouse on a stick and moving beyond a small threshold, it switches into drag/rotate mode instead of treating it as a click.

4. **While stickBeingPlaced != null**
   - Left click: confirms current pose, then enters manipulation mode (rotation around anchor or free rotation).
   - ESC: cancels placement, disconnects and deletes the stick, and returns one stick to inventory.

### Selection visual: outline-based

- Selection used to create a separate `SelectionMarker` GameObject. This has been replaced.
- Now selection is purely visualized via `SelectableOutline`:
  - On selecting a ball or stick, `InteractionManager`:
    - Calls `SelectableOutline.SetSelected(false)` on any previously selected object.
    - Calls `SelectableOutline.SetSelected(true)` on the newly selected object.
  - `ClearSelection()` resets outlines on any previous selection.

Note: only `AllocatableBall` and `Stick` have `SelectableOutline` attached by default, because these are the deletable/interactive ones in Select mode.

---

## UI Layer

### BuildToolbar (bottom HUD)

File: `Assets/Scripts/UI/BuildToolbar.cs`

- Creates its own `Canvas` (ScreenSpaceOverlay) and `EventSystem` at runtime.
- Bottom-centered panel with `HorizontalLayoutGroup`.
- Buttons:
  - **Select**: text `"选中/X删除"` → calls `InteractionManager.SetSelectTool()`.
  - **Ball**: shows `"球: n"` → calls `SetBallTool()`.
  - **Three stick buttons**: lengths from `stickLengths` array (default `{2,4,6}`) → call `SetStickTool(length)`.
- Counts:
  - If `Backpack.Instance` exists: uses Backpack counts per length.
  - Else: uses `GameManager.allocatableBallCount` and `GameManager.stickCount` as generic counts.

### WarehouseUI

File: `Assets/Scripts/UI/WarehouseUI.cs`

- Drawn in world space using `SpriteRenderer`s and `BoxCollider2D`s.
- When open, shows one slot per warehouse stick length (from `GameManager.GetWarehouseSnapshot()`).
- On click in world space, selects a `WarehouseSlot` and calls `GameManager.TryWithdrawStick(slot.stickLength, worldPos, out stick)`.

### BackpackHUD (test scene only)

File: `Assets/Scripts/UI/BackpackHUD.cs`

- Only used in `BackpackSystemTest` scene.
- Displays Backpack contents in the top-left HUD.

### Other UI scripts

- `WorldButton` — a minimal world-space button wrapper (click = invoke `Action OnClick`).  
- `ResourceHUD` / `BackpackHUD` / `BuildToolbar` together present resources and tools to the player.

---

## Inventory System: Backpack (optional)

File: `Assets/Scripts/Systems/Backpack.cs`

- Singleton `Backpack.Instance` (optional; main scene may not have it).
- Tracks:
  - `BallCount` — number of allocatable balls.
  - Stick counts per length: `Dictionary<int length, int count>`.
- Provides methods for adding/consuming balls and sticks.
- `GameManager` code always checks for `Backpack.Instance` and mirrors changes if present; otherwise it falls back to its own `allocatableBallCount` / `stickCount`.

---

## Controls Summary (Player Perspective)

From `README.md` and `InteractionManager` / `GameManager` logic:

- **Mouse Left**:
  - In Stick tool:
    - Click a ball → spawn stick anchored to that ball and enter placement.
    - Click-drag a stick → drag or rotate it depending on its connections.
  - In Ball tool:
    - Click near a free stick end → install an `AllocatableBall` on that stick end (if inventory allows).
  - In Select tool:
    - Click a stick or allocatable ball → select it (outline shown).
    - Click empty space → clear selection.

- **Mouse Right**:
  - Click a non-fully-connected stick → store it in warehouse.
  - Click empty space ��� toggle warehouse UI (if `WarehouseUI` exists).

- **Keyboard**:
  - `X` — delete the currently selected stick or allocatable ball (in any tool, as long as not placing/manipulating).
  - `Space` — toggle Build / Simulate phase.
  - `Esc` — toggle game pause (unless placing/manipulating); also cancels current stick placement when applicable.
  - `R` — restart current scene.
  - `W/A/S/D` or right mouse drag — move camera (handled by `CameraController`).
  - Mouse wheel — zoom camera.

---

## Agent Notes & Extension Points

When implementing new features, keep these points in mind:

1. **Selection & Deletion**
   - Only `AllocatableBall` and `Stick` are meant to be deletable by the player.
   - If you introduce a new deletable object type that should work with the Select + X workflow, you must:
     - Decide how `InteractionManager` discovers and stores its selection.
     - Attach `SelectableOutline` to it and integrate it into `TryDeleteSelection()`.

2. **New ball types**
   - Inherit from `Ball` and override `OnConnectedToInitialChanged` for custom effects.
   - Add visual feedback via the local `SpriteRenderer` in that override.

3. **New stick lengths**
   - Unlock via `GameManager.UnlockStickLength(length)` or prepopulate `availableStickLengths`.
   - Update `BuildToolbar.stickLengths` if you want them consistently exposed in UI.

4. **Physics behavior**
   - Respect the Build vs Simulate modes: any new physics-driven interactions should be disabled in Build mode and re-enabled in Simulate mode, following the patterns in `EnterBuild` / `EnterSimulate`.

5. **Runtime sprites & materials**
   - Prefer using `GameManager.MakeCircleSprite` and `MakeRectSprite` for simple visuals.
   - If you add new sprite-based objects that should support outlining, attach `SelectableOutline` and ensure they use a standard SpriteRenderer-compatible shader.

6. **Inventory-aware spawning**
   - Always check counts via `GameManager.TryConsumeAllocatableBalls` or `TryConsumeGenericStick` (and/or `Backpack`) before spawning new resources.

This summary should give an AI agent enough structure to locate the right scripts, follow existing patterns, and extend or debug the system without breaking core gameplay loops.
