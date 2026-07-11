## Context

Fireboy & Watergirl is a cooperative 2D platformer built in Unity 6 (6000.3.16f1) using URP 2D. Two characters (Fireboy and Watergirl) navigate levels together, solving puzzles with doors, pulley platforms, pushable rocks, and gem collection while avoiding elemental hazards (lava, water, green goo).

**Current architecture:**
- `StandardPlayerMovement.cs` (323 lines) handles all movement in `Update()` — reads raw keyboard keys directly via `Keyboard.current` and `Input.GetKey` fallback. Fireboy uses Arrow keys, Watergirl uses WASD. No InputAction assets are used despite the Input System package being installed.
- `GameManager.cs` is a local singleton managing score (`redGemsCollected`/`blueGemsCollected`), timer (`timeElapsed`), and win/lose via `FindObjectsByType` + UnityEvents.
- `PlayerHealth.cs` triggers `GameManager.LoseGame()` on contact with wrong liquid type.
- `PulleySystem.cs` (168 lines) implements weight-based connected platforms with physics (`MovePosition` in `FixedUpdate`) and chain rendering via LineRenderers.
- `PushableRock` uses `AddForce` with mass/friction physics.
- Camera is completely static per level — no camera scripts exist.
- `PauseMenuManager.cs` uses `Time.timeScale = 0` to pause.
- Scene loading is direct `SceneManager.LoadScene()` calls from UI buttons.
- No networking packages are installed. `com.unity.multiplayer.center` is only the planning dashboard.

## Goals / Non-Goals

**Goals:**
- Implement Photon Fusion Host-Client networking that works over WebSockets for WebGL deployment.
- Synchronize player movement, world objects, and game state for a consistent co-op experience.
- Provide a lobby system where one player hosts (gets a room code) and another joins.
- Build a per-player camera follow system for online play.
- Preserve local co-op mode as a fallback (no network required for same-keyboard play).

**Non-Goals:**
- Dedicated authoritative backend servers (host-client relay is sufficient for 2-player co-op).
- Rewriting the physics engine — use Unity physics synced by Fusion.
- Complex matchmaking (skill-based, ranked, etc.).
- Mobile-specific networking (focus on WebGL; mobile can come later).
- Spectator mode or replay system.

## Decisions

### 1. Network Topology: Host-Client (NOT Shared Mode)

**Decision:** Use **Host-Client** topology where the host is authoritative over all world state.

**Why not Shared Mode:** The original plan was ambiguous ("Shared Mode or Host-Client"). We commit to Host-Client because:
- World objects (pulleys, rocks, gems, doors) need a single authority to prevent race conditions (e.g., two players collecting the same gem simultaneously).
- GameManager state (score, timer, win/lose) needs one source of truth.
- Shared Mode's per-player authority model is awkward when both players interact with the same physics objects (pushing the same rock, standing on the same pulley).

**Trade-off:** Slightly more latency for the non-host player (client-side prediction mitigates this). Host disconnect kills the session (acceptable for friend-based co-op).

### 2. Network Transport: WebSocket (Fusion WebGL Transport)

**Decision:** Configure Fusion to use its WebSocket transport layer.

**Rationale:** WebGL does not support raw UDP/TCP. Fusion's WebSocket transport is the only option for browser deployment. Must verify this transport is stable in the current Fusion SDK version for Unity 6.

### 3. Input Architecture: Fusion NetworkInput (replace raw keyboard reads)

**Decision:** Replace all raw `Keyboard.current` / `Input.GetKey` reads with Fusion's `NetworkInput` system.

**Approach:**
```
┌─────────────────────────────────────────────────────────────┐
│ CURRENT: Direct keyboard read in Update()                   │
│                                                             │
│  Update() {                                                 │
│    if (Keyboard.current.leftArrowKey.isPressed) → move      │
│    if (Keyboard.current.upArrowKey.wasPressedThisFrame)     │
│      → jump                                                 │
│  }                                                          │
├─────────────────────────────────────────────────────────────┤
│ NEW: Fusion NetworkInput pipeline                           │
│                                                             │
│  struct PlayerInputData : INetworkInput {                   │
│    float horizontal;  // -1, 0, +1                          │
│    NetworkBool jumpPressed;                                  │
│  }                                                          │
│                                                             │
│  OnInput() {   // called by Fusion each tick                │
│    read Keyboard.current → fill PlayerInputData             │
│    runner.SetInput(inputData);                              │
│  }                                                          │
│                                                             │
│  FixedUpdateNetwork() {  // replaces Update()               │
│    GetInput(out PlayerInputData data);                      │
│    apply movement using data                                │
│  }                                                          │
└─────────────────────────────────────────────────────────────┘
```

Both players use the same keys (arrows or WASD — configurable per preference) since each is on their own keyboard/device. The `PlayerType` enum no longer determines input mapping — it determines character model and elemental properties.

### 4. Player Spawning & Assignment: Host = Fireboy, Client = Watergirl

**Decision:** The host always plays Fireboy; the joining client always plays Watergirl.

**Rationale:** Simplest approach for a 2-player co-op. The host creates the session and spawns as Fireboy. When the client joins, they spawn as Watergirl. Character selection can be added later as a separate change.

### 5. Camera: Per-Player Follow Camera (Built From Scratch)

**Decision:** Build a new `NetworkCameraFollow` script that follows the local player's character with smooth lerp and level-bounds clamping.

**Approach:**
- In online mode: camera follows local player only
- In local co-op mode: preserve current static camera behavior (both players visible)
- Camera applies a dead zone so small movements don't cause jitter
- Camera clamps to level bounds so it never shows outside the play area

### 6. Game State Management: Networked GameManager

**Decision:** Refactor `GameManager` to use `[Networked]` properties for shared state.

**Networked state:**
- `[Networked] int RedGems { get; set; }`
- `[Networked] int BlueGems { get; set; }`
- `[Networked] float ElapsedTime { get; set; }`
- `[Networked] NetworkBool IsGameActive { get; set; }`
- Win/lose triggered by host only, propagated via `[Networked]` game state or RPC

### 7. Scene Loading: Fusion NetworkSceneManager

**Decision:** Use Fusion's `NetworkSceneManager` so the host controls level transitions and all clients load synchronously.

**Flow:**
```
Host picks Level_02 → Fusion.NetworkSceneManager.LoadScene("Level_02")
                    → Client automatically loads Level_02
                    → Both spawn into level when scene ready
```

### 8. Pause System: Input Freeze (NOT Time.timeScale)

**Decision:** Replace `Time.timeScale = 0` pause with a local input-freeze + overlay approach.

**Rationale:** `Time.timeScale = 0` halts all Unity systems including Fusion's network tick. In multiplayer, pausing must be local-only: the pausing player sees an overlay and their inputs are suppressed, but the network keeps running. If both players pause, the game effectively pauses (no inputs from either side).

### 9. Death Handling: Synchronized Game Over

**Decision:** When any player dies, the host triggers `LoseGame()` which is propagated as networked state. Both players see the death animation and game-over screen.

**Flow:** Client's `PlayerHealth` detects death → sends RPC to host → host sets `[Networked] GameState = Lost` → all clients react to state change → show game over UI.

### 10. Hosting: WebGL Build → Vercel

**Decision:** Build to WebGL, output static files, deploy to Vercel. Add `vercel.json` for routing if needed. Gzip/Brotli compression for build assets to reduce load time.

## Risks / Trade-offs

- **Risk:** Photon Fusion + Unity 6 + WebGL compatibility is unverified.
  - *Mitigation:* Run a compatibility spike (Task 1.1) before any implementation. If Fusion doesn't support Unity 6 WebGL, evaluate Photon PUN2 or Netcode for GameObjects as alternatives.

- **Risk:** WebGL network latency. Physics-based platforming suffers from input delay over WebSocket.
  - *Mitigation:* Use Fusion's client-side prediction and lag compensation. The host player has zero latency advantage; the client needs prediction to feel responsive. For a co-op game (not competitive), moderate latency is tolerable.

- **Risk:** Host disconnect kills the session.
  - *Mitigation:* Acceptable for 2-player co-op with friends. Return the remaining player to the main menu with a "Host disconnected" message.

- **Risk:** PulleySystem physics desync. Weight-based connected platforms with `MovePosition` are hard to sync without jitter.
  - *Mitigation:* Make the host authoritative for pulley positions. Client interpolates. Weight detection (which players are on which platform) is determined by the host only.

- **Risk:** WebGL build size increases significantly with Photon SDK.
  - *Mitigation:* Measure build size before and after. Apply code stripping. Consider lazy-loading the networking module if size is unacceptable.

- **Risk:** Pushable rock physics divergence. Two players pushing the same rock from different sides can desync easily.
  - *Mitigation:* Host is authoritative for rock physics. Client inputs are sent to host, host applies forces, state is synced back.

## Open Questions

1. **Local co-op preservation**: Should the game detect "no network" and fall back to the original same-keyboard mode seamlessly? Or should local/online be separate menu options?
2. **Level selection flow**: After both players are in the lobby, who picks the level? Only the host? Should there be a "ready up" system?
3. **Key rebinding in online mode**: Since each player is on their own keyboard, should we offer key rebinding? Or just default to arrow keys for both?
