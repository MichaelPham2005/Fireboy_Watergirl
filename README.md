<p align="center">
  <img src="Assets/Tileset/Level_Screenshots/level_01_screenshot.png" alt="Fireboy & Watergirl Banner" width="600"/>
</p>

<h1 align="center">🔥 Fireboy & Watergirl 💧</h1>

<p align="center">
  <strong>A Unity 2D Co-op Puzzle Platformer — Where Fire Meets Water</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Unity-6000.3.16f1-000000?logo=unity&logoColor=white" alt="Unity Version"/>
  <img src="https://img.shields.io/badge/C%23-12.0-239120?logo=csharp&logoColor=white" alt="C#"/>
  <img src="https://img.shields.io/badge/Photon%20Fusion-2-004480?logo=photon&logoColor=white" alt="Photon Fusion"/>
  <img src="https://img.shields.io/badge/Platform-WebGL-F16529?logo=webgl&logoColor=white" alt="WebGL"/>
  <img src="https://img.shields.io/badge/Deployment-Vercel-000000?logo=vercel&logoColor=white" alt="Vercel"/>
</p>

---

## 📖 Story

> *Inspired by the classic Fireboy & Watergirl browser game, our Unity 2D project reimagines the duo as lovers — opposites bound by fate.*

**Fireboy** commands fire. **Watergirl** commands water. Alone, each is vulnerable. Together, they are unstoppable.

Their journey unfolds across **4 levels** — each a trial that tests their bond:

| Level | Title | Description |
|:-----:|-------|-------------|
| 🏛️ **1** | *"First Steps Together"* | A gentle tutorial on movement and cooperation — learn to walk, jump, and work as one. |
| 🌊 **2** | *"Rising Tides & Flames"* | Complex traps emerge. Lava pools, water hazards, and precision jumps push the duo to their limits. |
| 🌑 **3** | *"The Shadowed Temple"* | Darkness descends. Pulleys, levers, and mechanical puzzles guard every path forward. |
| 👹 **4** | *"Final Trial"* | The Shadow Guardian awakens — an enemy that hunts both heroes. The hardest challenge awaits. |

---

## ✨ Key Features

### 1. 🎮 Dual Character System

Two characters, two elemental identities, one shared goal.

- **Fireboy** — Immune to lava, destroyed by water and green goo. Controlled with **Arrow Keys**.
- **Watergirl** — Immune to water, destroyed by lava and green goo. Controlled with **WASD**.
- Both characters feature independent **Head & Body animation controllers** with states for idle, run, jump, fall, and death.
- Smooth physics-based movement powered by Unity's **Rigidbody2D** with slope detection, ground checking, and velocity-based head tilting.
- Players can switch focus with character toggle SFX for tactile feedback.

### 2. ⏱️ Time-Based Ranking System

Every second counts. Levels are timed from start to finish, and your completion time earns a letter rank:

| Rank | Time Threshold |
|:----:|:--------------:|
| **A** ⭐ | ≤ 60 seconds |
| **B** | ≤ 90 seconds |
| **C** | ≤ 120 seconds |
| **D** | > 120 seconds |

- **Top 5 best times** per level are saved persistently via JSON serialization to `PlayerPrefs`.
- The Ranking Panel in the main menu displays your best rank for each of the 4 levels.
- Levels unlock **sequentially** — complete Level *N* to unlock Level *N+1*.

### 3. 🧩 Interactive Mechanics & Puzzles

A rich library of cooperative puzzle elements that demand teamwork:

| Mechanic | Description |
|----------|-------------|
| **Lever Switches** | Physical levers pushed by player collision — rotate between two angles to toggle connected gates open/closed. |
| **Pressure Buttons** | Floor plates that detect standing weight (players or pushable rocks). Gates stay open only while pressure is applied. |
| **Pulley Systems** | Counterweight platforms — the heavier side drops, the lighter side rises. Features procedural chain rendering via `LineRenderer` with scrolling UV textures. |
| **Pushable Rocks** | Physics-driven objects that can be pushed onto buttons or used as stepping stones. Network-synced with damping and speed limits. |
| **Gates** | Kinematic barriers controlled by levers and buttons. Support reference counting so multiple switches can control a single gate. |
| **Falling Stalactites** | Dynamic ceiling hazards that randomly target players, shake as a warning, then drop — shattering into physics debris on impact. |
| **Elemental Pools** | Lava (red), Water (blue), and Goo (green) liquid hazards with animated surface waves powered by a **custom HLSL shader** (`WaterChopURP`). |
| **Exit Doors** | Color-coded doors (🔴 Fire / 🔵 Water) — **both players must reach their respective door simultaneously** to win. |

### 4. 🔊 Full Audio System & Polished UI/UX

#### Audio
A dedicated `AudioManager` singleton persists across scenes (`DontDestroyOnLoad`) and provides:

- **7 Background Music tracks** — menu theme, level BGM, dark level variant, speed variant, level finish, and game over music.
- **29+ Sound Effects** — footsteps (per character), jumps, gem pickups, door opens, lever pulls, platform rumbles, portal effects, death, win/lose stingers, clock ticking, wind, freeze, melt, and more.
- Separate **BGM and SFX volume sliders** with real-time adjustment.
- Character-specific SFX (Fireboy and Watergirl have distinct jump and footstep sounds).

#### UI/UX
- **Main Menu** with animated button hover effects (scale, bold, brightness) and full keyboard/mouse navigation support via `MenuNavigator`.
- **In-Game HUD** displaying a live timer in `MM:SS` format.
- **Pause Menu** with continue, retry, and home options — freezes `Time.timeScale` in local mode, suppresses input in online mode.
- **Win Panel** showing completion time, gem counts (🔴/🔵), letter rank, and next level button.
- **Game Over Panel** with retry and home options.
- **Level Lock Modal** dynamically generated when attempting locked levels.

### 5. 💎 Gem Collection & Customization

#### Gems
- **Red Gems** (🔴) — collectible only by **Fireboy**.
- **Blue Gems** (🔵) — collectible only by **Watergirl**.
- Tag-validated collection with pickup SFX and particle feedback.
- Gem counts are tracked by `GameManager` and displayed on the victory screen.
- Network-synced collection state ensures both players see pickups in online mode.

#### Character Customization
- **Fireboy** can equip **Ties** (White, Blue, Pink, Green color variants).
- **Watergirl** can equip **Scarves** (White, Blue, Pink, Green color variants).
- Selections are saved to `PlayerPrefs` (`FB_Tie`, `WG_Tie`) and persist across sessions.
- Accessories are instantiated and color-tinted at runtime on the character sprite.
- The main menu features a dedicated **Dress-Up Panel** with dynamically generated item selection UI.

### 6. 👻 Enemy — The Shadow Guardian

The **Shadow Guardian** is an AI-driven enemy featured in Level 4: *"Final Trial"*.

- **Target Switching** — periodically alternates between hunting Fireboy and Watergirl on a configurable timer (`switchInterval`).
- **Visual Warning** — the Guardian's sprite tint changes to match its current target (🔴 red when hunting Fireboy, 🔵 blue when hunting Watergirl).
- **Seek Behavior** — moves toward the active target at a configurable speed.
- **Lethal Contact** — triggers instant death on collision with either player.
- Features a dedicated **animation controller** (`Shadow_Guardian.controller`) and sorting layer (`Guardian`).

---

## 🛠️ Tech Stack & Architecture

### Core Technologies

| Technology | Version | Purpose |
|-----------|---------|---------|
| **Unity** | 6000.3.16f1 (Unity 6) | Game engine — 2D renderer, physics, animation, scene management |
| **C#** | 12.0 | Primary programming language |
| **Universal Render Pipeline (URP)** | 17.3.0 | Modern rendering pipeline with 2D lighting support |
| **Unity Input System** | 1.19.0 | New Input System for dual-player keyboard input mapping |
| **Photon Fusion 2** | — | Real-time networking framework for online 2-player co-op |
| **TextMesh Pro** | — | Advanced text rendering for UI elements |
| **Unity 2D Tilemap** | 1.0.0 | Tile-based level construction with custom tile palettes |
| **Unity 2D Animation** | 13.0.5 | Sprite-based character and environment animation |
| **Unity Timeline** | 1.8.12 | Cutscene and sequence management |

### Deployment

| Platform | Technology |
|----------|-----------|
| **WebGL** | Unity WebGL build with gzip/brotli compression |
| **Hosting** | Vercel with custom CORS headers (`Cross-Origin-Opener-Policy`, `Cross-Origin-Embedder-Policy`) |

### Custom Shader

A **custom HLSL shader** (`WaterChopURP`) renders animated liquid surfaces:
- Sinusoidal wave distortion with configurable flow speed, wave amplitude, and frequency.
- Edge protection masks to prevent visual artifacts at pool boundaries.
- Built for URP with proper `Universal2D` light mode tagging.

### Architecture & Design Patterns

```
Assets/Scripts/
├── Player/
│   ├── StandardPlayerMovement.cs   ← Physics controller, animation, input, accessories
│   └── PlayerHealth.cs             ← Elemental damage, death, network RPC proxy
├── Managers/
│   ├── GameManager.cs              ← Singleton level coordinator, timer, win/lose state
│   ├── AudioManager.cs             ← Persistent singleton audio controller (BGM + SFX)
│   ├── UIManager.cs                ← HUD, win/lose panels, rank calculation
│   ├── MainMenuManager.cs          ← Menu navigation, level selection, lock/unlock
│   ├── PauseMenuManager.cs         ← Pause flow (local timescale / online input suppression)
│   ├── PulleyManager.cs            ← Pulley system orchestration
│   ├── SaveSystem.cs               ← JSON serialization to PlayerPrefs (times + ranks)
│   ├── MenuButtonHover.cs          ← Button hover animations (scale, bold, brightness)
│   └── MenuNavigator.cs            ← Keyboard/mouse menu navigation
├── Obstacles/
│   ├── ButtonSwitch.cs             ← Pressure plate detection with OverlapBox sensor
│   ├── LeverSwitch.cs              ← Physical lever with kinematic rotation
│   ├── PulleySystem.cs             ← Counterweight platforms with procedural chain rendering
│   ├── PulleyPlatform.cs           ← Individual platform weight detection
│   ├── Gate.cs                     ← Kinematic barrier with reference-counted signals
│   ├── Gem.cs                      ← Tag-validated collectible with network sync
│   ├── Pushable.cs                 ← Network-synced physics pushable rock
│   ├── FallingStalactite.cs        ← Dynamic hazard with debris VFX
│   ├── ShadowGuardianAI.cs         ← Enemy AI with target-switching seek behavior
│   ├── LiquidAnimation.cs          ← Liquid surface animation
│   └── PoolElement.cs              ← Elemental pool configuration
├── Door/
│   └── DoorController.cs           ← Exit door readiness sensor with network sync
├── Network/
│   ├── NetworkRunnerController.cs  ← Photon Fusion host/client lifecycle manager
│   ├── PlayerSpawner.cs            ← Network player instantiation
│   ├── NetworkCameraFollow.cs      ← Networked camera tracking
│   ├── GameModeManager.cs          ← Local/Online mode switching
│   ├── PlayerInputData.cs          ← Network input data structure
│   └── FusionConnectionTest.cs     ← Network connection testing
├── Custom/
│   ├── DressUpManager.cs           ← Character customization with persistent saves
│   └── EquipButton.cs              ← Customization UI button handler
├── UI/
│   └── LobbyUI.cs                  ← Online lobby interface
└── Visualizer/
    └── ChainVisualizer.cs          ← Procedural chain line rendering
```

**Key Design Patterns Used:**

| Pattern | Usage |
|---------|-------|
| **Singleton** | `GameManager`, `AudioManager`, `MainMenuManager`, `NetworkRunnerController` |
| **Observer / Event-Driven** | UnityEvents for win/lose, Photon Fusion callbacks, collision events |
| **State Machine** | Game states (`Playing`, `Won`, `Lost`), animation controllers, stalactite lifecycle |
| **Strategy / Adapter** | Input & physics switching between Local Co-op and Online Multiplayer modes |
| **Command Receiver** | Gates receive open/close signals from switches |
| **Reference Counting** | Gates track multiple switch signals for correct state |
| **Proxy** | `PlayerHealth` acts as RPC gateway for scene loading |
| **DAO / Repository** | `SaveSystem` abstracts persistent data access |
| **Facade** | `AudioManager` wraps complex audio source management behind simple API calls |

---

## 🎮 How to Play

### Controls

| Action | Fireboy (🔴) | Watergirl (🔵) |
|--------|:-----------:|:-------------:|
| Move Left | ← Arrow | A |
| Move Right | → Arrow | D |
| Jump | ↑ Arrow | W |

### Rules
- 🔴 **Fireboy** can walk through lava but is destroyed by water and green goo.
- 🔵 **Watergirl** can walk through water but is destroyed by lava and green goo.
- 💀 **Green Goo** is lethal to both characters.
- 🚪 Both players must reach their respective exit doors **at the same time** to complete a level.
- 💎 Collect gems matching your character's element for bonus score.
- ⏱️ Complete levels as fast as possible for a higher rank!

### Game Modes
- **Local Co-op** — Two players share one keyboard.
- **Online Multiplayer** — Host creates a 4-character room code; the other player joins using the code. Powered by Photon Fusion 2.

---

## 🚀 Getting Started

### Prerequisites
- **Unity 6** (version `6000.3.16f1` or compatible)
- **Photon Fusion 2** App ID (for online multiplayer — configure in `Assets/Photon/Fusion/` settings)

### Setup
1. **Clone the repository:**
   ```bash
   git clone https://github.com/MichaelPham2005/Fireboy_Watergirl.git
   ```
2. **Open in Unity Hub** — Add the project and open with Unity 6000.3.16f1.
3. **Import Photon Fusion** — If not already included, import the Photon Fusion 2 SDK and set your App ID.
4. **Build Settings** — Ensure scenes are added in order:
   - `Home` (index 0)
   - `Level_01` (index 1)
   - `Level_02` (index 2)
   - `Level_03` (index 3)
   - `Level_04` (index 4)
5. **Play** — Hit Play in the Unity Editor or build for WebGL.

### WebGL Deployment (Vercel)
The project includes a pre-configured [`vercel.json`](vercel.json) with proper CORS and content-encoding headers for Unity WebGL builds:
```bash
# Build WebGL in Unity, then deploy:
vercel --prod
```

---

## 📁 Project Structure

```
Fireboy_Watergirl/
├── Assets/
│   ├── Animation/          # Animator controllers & clips (Fireboy, Watergirl, Enemy, Environment)
│   ├── Audio/
│   │   ├── Music/          # 7 BGM tracks (menu, level, dark, speed, finish, over)
│   │   └── SFX/            # 29+ sound effects
│   ├── Photon/             # Photon Fusion 2 SDK (networking)
│   ├── Prefabs/            # Player, door, gem, obstacle, and UI prefabs
│   ├── Resources/          # Customization assets (ties, scarves, render textures)
│   ├── Scenes/             # Home + 4 level scenes
│   ├── Scripts/            # All game logic (see Architecture section)
│   ├── Settings/           # URP renderer & pipeline settings
│   ├── Tile Map/           # Tilemap prefabs (Background, Wall, Lava, Water, Goo)
│   ├── Tileset/            # Sprite assets organized by category (22 folders)
│   └── WaterChopShader.shader  # Custom HLSL liquid animation shader
├── Build/                  # WebGL build output
├── Packages/               # Unity package manifest
├── ProjectSettings/        # Unity project configuration
├── vercel.json             # Vercel deployment configuration
└── README.md               # You are here!
```

---

## 👥 Team

Built with ❤️ and 🔥 as a Unity 2D game development project.

---

<p align="center">
  <em>"Alone, each is vulnerable. Together, they are unstoppable."</em>
</p>
