# Game Systems Analysis: Ranking, Online Multiplayer & UI Positioning

This document provides a detailed breakdown of the current implementation for the Ranking System, Online Multiplayer mechanisms, and the dynamic UI positioning of the Score/Config screens based on the current codebase.

---

## 1. Ranking & Save Mechanism
The game utilizes a time-based ranking system, which is calculated and persisted using Unity's `PlayerPrefs` alongside JSON serialization.

### 1.1 Calculation Logic
When players reach both doors and trigger the win condition, `UIManager.cs` calculates their rank based on the total `TimeElapsed`:
- **Rank A (1)**: Completed in ≤ 60 seconds.
- **Rank B (2)**: Completed in ≤ 90 seconds.
- **Rank C (3)**: Completed in ≤ 120 seconds.
- **Rank D (4)**: Completed in > 120 seconds.

### 1.2 Saving Data (`SaveSystem.cs`)
The game defines a `LevelData` class containing:
- `List<float> topTimes`: A list tracking the 5 best completion times (sorted ascending).
- `int bestRank`: The highest achieved rank (where 1 is the best).

When a level is completed, `GameManager.cs` triggers two static methods in `SaveSystem.cs`:
1. `SaveSystem.SaveTime(levelName, time)`: Adds the new time, sorts the list, removes any times beyond the top 5, serializes the `LevelData` object to JSON using `JsonUtility`, and saves it to `PlayerPrefs` under the key `"LevelData_[LevelName]"`.
2. `SaveSystem.SaveRank(levelName, rankNum)`: Checks if the new numeric rank is lower (better) than the stored `bestRank`. If so, it overwrites the value and resaves the JSON to `PlayerPrefs`.

---

## 2. Online Multiplayer Mechanism
The online multiplayer architecture is built entirely on **Photon Fusion**, utilizing a Host-Client topology and state authority synchronization.

### 2.1 Game Mode Toggling (`GameModeManager.cs`)
The game operates under two primary modes: `LocalCoop` and `OnlineMultiplayer`. This mode dictates which variables and logic branches are executed across managers.

### 2.2 Network Synchronization
In `GameManager.cs`, state variables are duplicated. One set is for Local Co-op, while the other uses Fusion's `[Networked]` attribute for automatic state synchronization across the network:
```csharp
// Local State
public float timeElapsedLocal;
public int redGemsCollectedLocal;

// Networked State
[Networked] public float NetworkTimeElapsed { get; set; }
[Networked] public int NetworkRedGems { get; set; }
```
The game abstracts this by exposing unified getters (e.g., `TimeElapsed`, `RedGemsCollected`) that dynamically return either the local variable or the networked variable depending on `GameModeManager.CurrentMode`.

### 2.3 RPCs (Remote Procedure Calls)
Important game events (like collecting gems or dying) are handled via RPCs. If a Client (without State Authority) collects a gem, it fires an RPC to the Host (`RpcTargets.StateAuthority`), which increments the `[Networked]` counter. Fusion then automatically replicates this updated value back to all clients.

### 2.4 Network Spawning & Scene Loading
The network session is initialized by `NetworkRunnerController.cs`. Scene transitions (e.g., clicking Retry or Next Level) use `Runner.LoadScene()` instead of standard Unity SceneManagement. This ensures that the Host forces all connected clients to load the new scene synchronously.

---

## 3. UI Positioning: Score Screen & Config Buttons
The UI components are managed programmatically by `UIManager.cs`, which dynamically binds and positions UI elements at runtime to ensure cross-scene compatibility without needing manual prefab overrides.

### 3.1 The Win Panel (Màn hình tính điểm)
The `WinPanel` and `GameOverPanel` are located inside a Canvas prefab named `MenuHandler`. 
When the game is won, `UIManager.ShowWinScreen()` activates the `WinPanel`, pauses time (`Time.timeScale = 0f` for local co-op), and dynamically populates the `CurrentTimerText`, `RedGemCountText`, `BlueGemCountText`, and `RankText`.

### 3.2 Dynamic Button Positioning (Nút Config / Next / Retry)
The positioning of the navigation buttons inside the `WinPanel` is **dynamically adjusted based on the current level number** via the `ConfigureWinPanelButtons()` method:
- **For Levels 1 to 3**: 
  - The `NextLevelButton` is visible.
  - The `Retry` button is offset to the side using `rectTransform.anchoredPosition = new Vector2(125f, y)` to make room for the Next button.
- **For Level 4 (The final level)**:
  - The `NextLevelButton` is programmatically hidden (`SetActive(false)`).
  - The `Retry` button is **moved to the exact center** of the screen by setting its X-anchor to 0: `rectTransform.anchoredPosition = new Vector2(0f, y)`.

This dynamic positioning approach allows the same `MenuHandler` prefab to be used across all levels while automatically adapting its layout to fit the context (preventing players from clicking "Next" on the final level).
