## ADDED Requirements

### Requirement: Tick-Based Networked Movement
The system SHALL process all character movement in Fusion's `FixedUpdateNetwork()` tick instead of Unity's `Update()` frame loop.

#### Scenario: Player moves character
- **WHEN** a player inputs horizontal movement or jump commands
- **THEN** the movement is applied during the Fusion network tick and synchronized across the network

### Requirement: NetworkInput Pipeline
The system SHALL collect player input via Fusion's `OnInput()` callback using a `PlayerInputData` struct, replacing direct `Keyboard.current` and `Input.GetKey` reads.

#### Scenario: Input collection
- **WHEN** Fusion requests input each tick
- **THEN** the local keyboard state is captured into a `PlayerInputData` struct containing horizontal direction and jump state

#### Scenario: Input application
- **WHEN** `FixedUpdateNetwork()` executes
- **THEN** the player character moves based on the `PlayerInputData` retrieved via `GetInput()`

### Requirement: Networked Character State
The system SHALL synchronize character position, velocity, and animation state using `[Networked]` properties and `NetworkTransform`.

#### Scenario: Remote player observation
- **WHEN** a player's character moves on their local client
- **THEN** the other player sees the movement reflected with smooth interpolation and minimal lag

### Requirement: Local Authority Enforcement
The system SHALL ensure each player can only control their assigned character (host = Fireboy, client = Watergirl).

#### Scenario: Authority check on input
- **WHEN** input is processed for a character
- **THEN** the system only accepts input from the player who has state authority over that character

### Requirement: Client-Side Prediction
The system SHALL implement client-side prediction for the non-host player to mask network latency.

#### Scenario: Client moves with prediction
- **WHEN** the client player inputs movement
- **THEN** the character moves immediately on the client's screen, with server reconciliation correcting any mispredictions

### Requirement: Slope and Jump Physics Preservation
The system SHALL preserve existing movement behavior including slope handling, double-jump prevention, and gravity within the networked tick system.

#### Scenario: Player traverses slope
- **WHEN** a player's character moves across a sloped surface
- **THEN** the movement projects onto the slope normal, matching single-player behavior

#### Scenario: Double jump prevention
- **WHEN** a player attempts to jump while already airborne
- **THEN** the jump is rejected, preserving the single-jump-only mechanic
