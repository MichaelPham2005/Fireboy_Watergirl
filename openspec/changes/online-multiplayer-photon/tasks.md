## 1. Compatibility Spike & SDK Setup

- [x] 1.1 Verify Photon Fusion SDK compatibility with Unity 6 (6000.3.16f1) — download SDK, import into project, confirm no compile errors.
- [x] 1.2 Build a minimal WebGL test scene with Fusion's WebSocket transport to confirm networking works in the browser.
- [x] 1.3 Configure Fusion Hub settings: insert Photon App ID, set default topology to Host-Client, enable WebSocket transport.
- [x] 1.4 If Fusion is incompatible, evaluate PUN2 or Netcode for GameObjects and update the design accordingly (STOP and reassess). *(N/A — Fusion 2 is compatible)*

## 2. Lobby & Matchmaking UI

- [x] 2.1 Create a new `LobbyScene` with "Host Game" and "Join Game" buttons (separate from existing `Home` scene, or integrated as a new panel).
- [x] 2.2 Implement `NetworkRunnerController.cs` — a MonoBehaviour that manages the Fusion `NetworkRunner` lifecycle (start host, start client, shutdown).
- [x] 2.3 Implement room code generation: generate a short alphanumeric code (e.g., 4 characters) and use it as the Fusion session name.
- [x] 2.4 Implement the "Host Game" flow: start Fusion in Host mode → display room code → show "Waiting for player..." state.
- [x] 2.5 Implement the "Join Game" flow: text input for room code → start Fusion in Client mode → connect to session → show error on invalid code.
- [x] 2.6 Implement player assignment: host is assigned Fireboy, client is assigned Watergirl upon connection.
- [x] 2.7 Handle lobby-phase disconnects: host cancel destroys session; client disconnect returns host to waiting state.

## 3. Network Player Spawning & Prefab Conversion

- [x] 3.1 Convert `Fireboy_Player` prefab into a `NetworkObject` with `NetworkTransform` for position/rotation interpolation.
- [x] 3.2 Convert `Watergirl_Player` prefab into a `NetworkObject` with `NetworkTransform` for position/rotation interpolation.
- [x] 3.3 Implement `PlayerSpawner.cs` — spawns the correct character prefab when each player connects (host → Fireboy, client → Watergirl).
- [x] 3.4 Set up state authority: each player has authority over their own character's `NetworkObject`.

## 4. Movement Refactoring (StandardPlayerMovement → NetworkBehaviour)

- [x] 4.1 Create `PlayerInputData` struct implementing `INetworkInput` with `float horizontal` and `NetworkBool jumpPressed` fields.
- [x] 4.2 Implement `OnInput()` callback: read `Keyboard.current` (arrow keys or WASD — same keys for both players since they're on separate devices) → fill `PlayerInputData` → `runner.SetInput()`.
- [x] 4.3 Refactor horizontal movement from `Update()` to `FixedUpdateNetwork()` using `GetInput(out PlayerInputData)`.
- [x] 4.4 Refactor jump logic into `FixedUpdateNetwork()` — preserve single-jump-only and velocity-based airborne detection.
- [x] 4.5 Refactor slope handling into `FixedUpdateNetwork()` — preserve slope normal projection for smooth slope traversal.
- [x] 4.6 Convert movement-relevant state to `[Networked]` properties: `isGrounded`, `velocity`, `facingDirection`.
- [x] 4.7 Implement client-side prediction for the non-host player to mask input latency.
- [x] 4.8 Sync animation parameters (`Speed`, `yVelocity`, `IsGrounded`) as `[Networked]` properties or via `NetworkAnimator`.
- [x] 4.9 Preserve head-tilt rotation logic within the network tick.
- [x] 4.10 Add authority check: only process input for the character the local player controls.
- [ ] 4.11 Test movement end-to-end: host moves Fireboy smoothly, client moves Watergirl smoothly, each sees the other with interpolation.

## 5. Game State Manager Refactoring (GameManager → NetworkBehaviour)

- [x] 5.1 Convert `GameManager` to a `NetworkBehaviour` with `[Networked]` properties: `RedGems`, `BlueGems`, `ElapsedTime`, `IsGameActive`, `GameState` (enum: Playing, Won, Lost).
- [x] 5.2 Move timer increment to host-only logic in `FixedUpdateNetwork()`.
- [x] 5.3 Refactor `CheckWinCondition()` to run on host only — host checks both door states and sets `GameState = Won`.
- [x] 5.4 Implement `OnChanged` callbacks for `GameState`: all clients react to state transitions (show win/lose UI, trigger animations).
- [x] 5.5 Refactor `WinGame()`: host triggers win sequence → freeze players → snap to doors → play animations → all clients see results screen.
- [x] 5.6 Refactor `LoseGame()` / death sync: `PlayerHealth` detects death → sends RPC to host → host sets `GameState = Lost` → all clients show game over.
- [x] 5.7 Ensure `DoorController.cs` only reports readiness to the host (authority check on trigger events).

## 6. World Object Synchronization

- [x] 6.1 Convert `Gem_Red` and `Gem_Blue` prefabs to `NetworkObject`s — host validates collection, despawns gem NetworkObject on all clients.
- [x] 6.2 Add simultaneous-collection guard: host processes first collection event, ignores duplicates for same gem.
- [x] 6.3 Convert door prefabs (`Red_door`, `Blue_door`) to `NetworkObject`s — sync `isOpen` animator bool as `[Networked]` property.
- [x] 6.4 Convert `PulleySystem` to a `NetworkBehaviour` — host is authoritative for platform positions; client interpolates via `NetworkTransform` or manual sync.
- [x] 6.5 Network `PulleyPlatform` weight detection: host determines which players/rocks are on each platform; client does not run weight calculation.
- [x] 6.6 Sync pulley chain visuals (LineRenderer positions) — derive from platform positions on each client locally (no need to network LineRenderer data).
- [x] 6.7 Convert `PushableRock` to a `NetworkObject` with `NetworkRigidbody2D` — host is authoritative for rock physics.
- [x] 6.8 Handle both-players-push-same-rock: host receives force inputs from both clients, applies combined force.
- [ ] 6.9 Test all world objects end-to-end: gems collect once, doors sync open/close, pulleys move correctly, rocks push consistently.

## 7. Camera System (New)

- [x] 7.1 Create `NetworkCameraFollow.cs` — attaches to the main camera and follows the local player's character with smooth lerp.
- [x] 7.2 Implement dead zone: camera only moves when the player moves beyond a configurable threshold.
- [x] 7.3 Implement level-bounds clamping: define bounds per level (e.g., via a `BoxCollider2D` trigger or manual bounds) and clamp camera position.
- [x] 7.4 Add mode detection: if online mode → follow local player; if local co-op mode → use static camera (current behavior).
- [ ] 7.5 Test camera in each level: verify smooth following, no jitter, no out-of-bounds views.

## 8. Scene Loading & Level Flow

- [x] 8.1 Implement networked scene loading using Fusion's `NetworkSceneManager` — host triggers `LoadScene()`, all clients follow.
- [x] 8.2 Create a post-lobby level selection UI: host picks a level, both clients transition together.
- [x] 8.3 Refactor `UIManager.cs` "Next Level" and "Retry" buttons to use networked scene loading instead of direct `SceneManager.LoadScene()`.
- [x] 8.4 Refactor `MainMenuManager.cs` to offer "Local Co-op" and "Online Multiplayer" mode selection.
- [x] 8.5 Handle scene transition edge cases: what happens if a client disconnects during loading?

## 9. Pause & Disconnect Handling

- [x] 9.1 Refactor `PauseMenuManager.cs`: in online mode, replace `Time.timeScale = 0` with local input suppression + pause overlay (network tick continues).
- [x] 9.2 Preserve `Time.timeScale = 0` pause for local co-op mode only.
- [x] 9.3 Implement mid-game disconnect detection: host disconnect → client sees "Host disconnected" → return to main menu.
- [x] 9.4 Implement mid-game disconnect detection: client disconnect → host sees "Player disconnected" → return to main menu.
- [x] 9.5 Clean up Fusion runner on disconnect/return to menu to prevent orphaned network sessions.

## 10. Local Co-op Preservation

- [x] 10.1 Add a `GameMode` flag (enum: LocalCoop, OnlineMultiplayer) accessible globally.
- [x] 10.2 Ensure `StandardPlayerMovement` can still function in local co-op mode (Fireboy = arrows, Watergirl = WASD, no Fusion runner needed).
- [x] 10.3 Ensure `GameManager`, doors, gems, pulleys, and rocks all work without a Fusion runner when `GameMode = LocalCoop`.
- [ ] 10.4 Test local co-op mode is unaffected by all networking changes.

## 11. WebGL Build & Vercel Deployment

- [x] 11.1 Switch build target to WebGL in Unity Build Settings.
- [ ] 11.2 Configure IL2CPP code stripping to minimize WebGL build size.
- [ ] 11.3 Build the project and measure build size — document the increase from the Photon SDK.
- [ ] 11.4 Test the WebGL build in a browser: lobby flow, gameplay, scene transitions.
- [x] 11.5 Create `vercel.json` with routing configuration for the WebGL build folder structure.
- [ ] 11.6 Deploy to Vercel and verify end-to-end multiplayer works in a hosted environment.
