## Why

Fireboy & Watergirl currently only supports local co-op on a single keyboard (Fireboy on Arrow keys, Watergirl on WASD). Adding online multiplayer allows two players to connect over the internet and play together from separate devices. We will use Photon Fusion with Host-Client topology over WebSocket transport, targeting WebGL for deployment on Vercel.

## What Changes

- Integrate **Photon Fusion SDK** with WebSocket transport for WebGL-compatible networking.
- Implement a **Host-Client** architecture where the host is authoritative over world state; no dedicated backend server required.
- **Refactor `StandardPlayerMovement.cs`** from raw keyboard reads in `Update()` to a Fusion `NetworkBehaviour` using `NetworkInput` polling and `FixedUpdateNetwork()` for tick-based physics.
- **Refactor `GameManager.cs`** into a network-aware state manager with `[Networked]` properties for score, timer, and win/lose conditions.
- **Sync all interactive world objects**: doors (`DoorController`), gems (`Gem`), pulley platforms (`PulleySystem`/`PulleyPlatform`), and pushable rocks (`PushableRock`).
- **Build a camera follow system** from scratch (none currently exists) so each player's camera follows their own character.
- **Implement networked scene loading** so the host controls level selection and both clients transition together.
- **Rework pause and death mechanics** for network compatibility (`Time.timeScale=0` cannot be used in multiplayer; death must be synchronized).
- Implement a **lobby UI** with room code hosting/joining.

## Capabilities

### New Capabilities
- `photon-fusion-setup`: Integration and configuration of Photon Fusion SDK with WebSocket transport for WebGL.
- `lobby-and-matchmaking`: Room-code-based lobby system for hosting and joining sessions.
- `network-player-movement`: Tick-based networked character movement replacing the current `Update()`-driven raw keyboard input system.
- `network-game-state`: Networked game state management — timer, score, win/lose conditions, scene transitions, pause, and death sync.
- `world-object-sync`: Synchronization of all interactive game objects: doors, gems, pulley systems, and pushable rocks.
- `network-camera`: Per-player camera follow system (built from scratch) for online play.

### Modified Capabilities
- (None — no existing global specs to modify)

## Impact

- **`StandardPlayerMovement.cs`** (323 lines): Major refactoring — move physics from `Update()` to `FixedUpdateNetwork()`, replace raw `Keyboard.current` reads with Fusion `NetworkInput`, add `[Networked]` state properties.
- **`PlayerHealth.cs`** (88 lines): Death must trigger synchronized game-over state via networked GameManager.
- **`GameManager.cs`** (153 lines): Must become network-aware. Score, timer, win/lose conditions all need `[Networked]` properties or RPCs.
- **`DoorController.cs`** (43 lines): Win condition checks must only run on the host.
- **`Gem.cs`** (34 lines): Collection must be authoritative to prevent double-collection.
- **`PulleySystem.cs`** (168 lines) + `PulleyPlatform.cs` (48 lines): Complex physics-based weight mechanic needs authoritative position sync and visual interpolation.
- **`Pushable.cs`** (53 lines): Physics-driven rocks need `NetworkRigidbody2D` or manual authority-based sync.
- **`PauseMenuManager.cs`** (81 lines): `Time.timeScale=0` is incompatible with networking; needs redesign.
- **`UIManager.cs`** (118 lines) + `MainMenuManager.cs`** (58 lines): Level loading must use Fusion's `NetworkSceneManager` for synchronized transitions.
- **Build size**: WebGL build will increase by several MB due to Photon Fusion SDK. Load time impact should be measured.
- **New dependency**: Photon Fusion SDK (must verify Unity 6 compatibility).
