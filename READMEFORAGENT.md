## 034GameJam — Agent-Oriented Project Overview

This document is for AI agents (and human maintainers) to quickly understand the structure and current mechanics of this Unity project.

Engine: **Unity 2022.3 (2D)**  
Main scene in build: `Assets/Scenes/TestScene.unity`

High level: this is a 2D physics toy where the player uses **balls** and **sticks** to build a structure. The structure’s connectivity to the initial ball drives production/unlocks. Recent changes focus on: (1) prefab-driven sticks of multiple lengths, (2) a text-free radial build menu + bottom HUD, and (3) right-mouse camera control only.

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
  - Two end transforms: `endA`, `endB` as children; **for prefab sticks these are configured in the prefab per length**.
  - World length is determined by the prefab; for legacy sticks it is `transform.localScale.y`.

- Logical endpoints:
  - `Ball endpointA`, `Ball endpointB` store which balls are connected to each end.
  - `GetFreeEnds()` returns the unconnected ends; `FreeEnd` returns the first available.

- Placement / manipulation state:
  - `StartPlacement(Ball anchorBall)` — called by `GameManager.SpawnStick`:
    - Makes rigidbody kinematic, no gravity, set placement mode.
    - One end is anchored to `anchorBall`.
    - In `Update`, `UpdatePlacementPose()` orients the stick towards the mouse.
  - `ConfirmPlacementPose()` — stops placement but keeps kinematic; immediately followed by manipulation.
  - `FinishPlacement()` — exits placement, re-enables dynamic physics.
  - Drag/rotate:
    - `BeginDrag`, `BeginRotate`, `BeginRotateFromAnchor`, `EndManipulation` and `UpdateManipulationPose()` handle moving/rotating sticks while in **Stick tool**.

- Auto-connection (magnetic attraction):
  - `FixedUpdate()`:
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
  - Periodically destroys `AllocatableBall` instances that have `currentConnections == 0` and `hasBeenConnected == true`.

---

## Core Orchestration: GameManager

File: `Assets/Scripts/Core/GameManager.cs`

Responsibilities:

1. **Game phase & physics**
   - Enum `GamePhase { Build, Simulate }` and property `Phase`.
   - `EnterBuild()` sets all sticks and non-fixed balls to kinematic & gravityScale 0.
   - `EnterSimulate()` sets them back to dynamic & gravityScale 1.
   - Toggled via Space key in `Update()`.

2. **Pause & restart**
   - `IsGamePaused`, and ESC toggles pause (unless a stick is being placed/manipulated).  
   - `RestartScene()` bound to `R` key.

3. **Resources & inventory**
   - Base counts:
     - `allocatableBallCount`
     - `stickCount` (generic count if Backpack not present)
     - `availableStickLengths` (list of unlocked stick lengths).
   - `initialStickLengths` (serialized int array) provides initial unlocked stick lengths when the list is empty.
   - Interop with optional `Backpack`: when present, per-length stick counts & ball counts are synchronized.
   - Auto-refresh sticks: if `autoRefreshSticks` is true, periodically `AddGenericSticks(1)`.
   - `ReturnStick()` / `ReturnAllocatableBall()` are used when deleting placed items.

4. **Spawning sticks & balls**

   - **Stick spawning (now prefab-driven per length)**
     - `GameManager` exposes a serialized array `StickPrefabConfig[] stickPrefabs`, each entry `{ length, Stick prefab }`.
     - `availableStickLengths` / `initialStickLengths` drive which stick lengths exist in gameplay and in the radial menu.
     - `SpawnStick(Ball anchorBall, int length)`:
       - Consumes a generic stick (`TryConsumeGenericStick`) and, if Backpack exists, a stick of that length.
       - Looks up a prefab via `GetStickPrefabForLength(length)`:
         - If found → instantiates that prefab at the anchor ball position and calls `Stick.StartPlacement(anchorBall)`.
         - If not found → falls back to a code-generated stick (runtime-created GameObject, SpriteRenderer, BoxCollider2D, Rigidbody2D, Stick + EndA/EndB).
     - `TryWithdrawStick(int length, Vector2 spawnPos, out Stick stick)`:
       - Uses the same `GetStickPrefabForLength` lookup; warehouse-spawned sticks are also prefab-based when possible.

   - **Ball spawning**
     - `SpawnAllocatableBall(Stick stick)` / `TryInstallBallOnStickFreeEnd(Stick stick)`:
       - Both create `AllocatableBall` instances near a stick free end, consuming `allocatableBallCount`.
       - If `allocatableBallPrefab` is set, they instantiate this prefab; otherwise they use a simple code-generated circular ball.
       - `TryInstallBallOnStickFreeEnd` also attaches the ball to the stick’s free end.
     - Auto-ball helpers:
       - `TryAttachStickFreeEndsToExistingConnections`, `AutoSpawnBallOnStickFreeEnd` and static helpers `TryAttachFreeEndToNearbyBall` / `TryAttachFreeEndToNearbyStickEnd` attempt to connect free ends to existing balls or create bridging balls.

5. **Warehouse system**
   - `stickWarehouse` (`Dictionary<int length, int count>`) stores sticks of specific lengths.
   - `StoreStick(Stick stick)` stores free (not fully connected) sticks back into warehouse and returns them to inventory.
   - `TryWithdrawStick(int length, Vector2 spawnPos, out Stick stick)` spawns a physics stick in-world (prefab-based when configured).
   - `GetWarehouseSnapshot()` exposes current warehouse content for UI.

6. **Procedural sprites**
   - `MakeCircleSprite()` and `MakeRectSprite()` generate simple white textures at runtime for use by sprite renderers (sticks, balls, ground, etc.).

---

## Player Input & Tools: InteractionManager + RadialMenu

File: `Assets/Scripts/UI/InteractionManager.cs`

Singleton: `InteractionManager.Instance` (assigned in `Awake`).

### Build tools

Enum `BuildTool { Stick, Ball, Select, Delete }`, state fields:

- `currentTool` (default Stick), `currentStickLength` (default 2).
- `SetStickTool(int length)` — activates Stick tool and sets the length (used by `BuildToolbar` and `RadialMenu`).
- `SetBallTool()` — activates Ball tool.
- `SetSelectTool()` — activates Select tool.
- `SetDeleteTool()` — activates Delete tool (used by the radial menu delete segment).

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

2. **Middle mouse button (radial menu)**
   - `RadialMenu` (`Assets/Scripts/UI/RadialMenu.cs`) owns its own overlay `Canvas`.
   - A middle mouse click toggles `RadialMenu.IsOpen` and shows a circular menu centered on screen.
   - Items are built from current game state:
     - One **ball** item → `InteractionManager.SetBallTool()`.
     - One item per stick length from `GameManager.availableStickLengths` → `InteractionManager.SetStickTool(length)`.
     - A **delete** item → `InteractionManager.SetDeleteTool()`.
     - A **none/select** item → `InteractionManager.SetSelectTool()`.
   - Visual design:
     - Circular background using `GameManager.MakeCircleSprite()`.
     - Icons are simple shapes only: circles (ball) and rectangles (sticks/delete/none)。没有文字。

3. **Right mouse button**
   - Handled by `CameraController` (`Assets/Scripts/Core/CameraController.cs`).
   - Used exclusively for camera drag (plus WASD for movement, mouse wheel for zoom).
   - Right-click no longer deletes or stores sticks; all delete/store actions go through the tool system / warehouse.

4. **Left mouse button (primary interaction)**
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

     - If `currentTool == Delete`:
       - Deletes sticks and allocatable balls under the cursor, returning inventory via `GameManager`.

     - If `currentTool == Stick`:
       - If `pendingStick != null` → start drag or rotate on that stick.
       - Else → find nearest ball via `GameManager.FindNearestBall` and spawn a new stick anchored to it via `SpawnStick(anchor, currentStickLength)`.

   - While holding left mouse on a stick and moving beyond a small threshold, it switches into drag/rotate mode instead of treating it as a click.

5. **While `stickBeingPlaced != null`**
   - Left click: confirms current pose, then enters manipulation mode (rotation around anchor or free rotation).
   - ESC: cancels placement, disconnects and deletes the stick, and returns one stick to inventory.

### Selection visual: outline-based

- Selection is visualized via `SelectableOutline` on balls/sticks.
- `ClearSelection()` resets outlines on any previous selection.

---

## UI Layer

### BuildToolbar (bottom HUD)

File: `Assets/Scripts/UI/BuildToolbar.cs`

- Creates its own `Canvas` (ScreenSpaceOverlay) and `EventSystem` at runtime.
- Bottom-centered panel with `HorizontalLayoutGroup`.
- Purely icon + progress-bar based HUD for the main TestScene (no text labels here):
  - **Ball slot**: circular icon (from `GameManager.MakeCircleSprite`) + horizontal filled bar, showing ball inventory fraction (`allocatableBallCount` or Backpack `BallCount` vs 256).
  - **Stick slots**: one per configured `stickLengths` (default `{2,4,6}`)，使用不同长度的矩形图标（`MakeRectSprite`）+ 横向填充条，表示每种棒的库存。
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
- Displays Backpack contents in the top-left HUD, with text (this is acceptable in that debug scene).

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
- `GameManager` always checks for `Backpack.Instance` and mirrors changes if present; otherwise it falls back to its own `allocatableBallCount` / `stickCount`.

---

## Controls Summary (Player Perspective)

From `README.md` and `InteractionManager` / `GameManager` / `RadialMenu` logic:

- **Mouse Left**:
  - In Stick tool:
    - Click a ball → spawn stick anchored to that ball and enter placement.
    - Click-drag a stick → drag or rotate it depending on its connections.
  - In Ball tool:
    - Click near a free stick end → install an `AllocatableBall` on that stick end (if inventory allows).
  - In Select tool:
    - Click a stick or allocatable ball → select it (outline shown).
    - Click empty space → clear selection.
  - In Delete tool:
    - Click a stick or allocatable ball → delete it and return inventory.

- **Mouse Middle**:
  - Toggle radial build menu (pure shape-based UI) to pick Ball / Stick length / Delete / Select.

- **Mouse Right**:
  - Drag camera (handled by `CameraController`).

- **Keyboard**:
  - `X` — delete the currently selected stick or allocatable ball (in any tool, as long as not placing/manipulating).
  - `Space` — toggle Build / Simulate phase.
  - `Esc` — toggle game pause (unless placing/manipulating); also cancels current stick placement when applicable.
  - `R` — restart current scene.
  - `W/A/S/D` or right mouse drag — move camera.
  - Mouse wheel — zoom camera.

---

## Agent Notes & Extension Points

When implementing new features, keep these points in mind:

1. **Stick prefabs per length**
   - Main scene now expects **one prefab per stick length** (configured in `GameManager.stickPrefabs`).
   - If you add a new length, you should:
     - Create a new prefab with correct `Stick` setup and assign it in `stickPrefabs`.
     - Add that length to `initialStickLengths` / `availableStickLengths` and to `BuildToolbar.stickLengths` if you want it in HUD.

2. **Selection & deletion**
   - Only `AllocatableBall` and `Stick` are meant to be deletable by the player in main TestScene.
   - If you introduce a new deletable object type that should work with Select/Delete workflows, extend `InteractionManager` accordingly and attach `SelectableOutline`.

3. **New ball types**
   - Inherit from `Ball` and override `OnConnectedToInitialChanged` for custom effects.

4. **Physics behavior**
   - Respect the Build vs Simulate modes: any new physics-driven interactions should be disabled in Build mode and re-enabled in Simulate mode, following `EnterBuild` / `EnterSimulate` patterns.

5. **Inventory-aware spawning**
   - Always check counts via `GameManager.TryConsumeAllocatableBalls` or `TryConsumeGenericStick` (and/or `Backpack`) before spawning new resources.

This summary reflects the current prefab-driven stick system, text-free radial/menu UI, and camera/input behavior, so agents (and humans) can smoothly continue work on this project.
