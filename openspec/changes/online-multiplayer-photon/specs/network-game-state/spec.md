## ADDED Requirements

### Requirement: Networked Game Timer
The system SHALL synchronize the game timer from the host to all clients using a `[Networked]` property.

#### Scenario: Timer ticking
- **WHEN** the game is active
- **THEN** the host increments the timer and all clients display the same elapsed time

### Requirement: Networked Score
The system SHALL synchronize gem collection counts (red and blue) as `[Networked]` properties on the game state manager.

#### Scenario: Score update on collection
- **WHEN** a gem is collected by either player
- **THEN** the host updates the networked score and all clients see the updated count

### Requirement: Networked Win Condition
The system SHALL evaluate win conditions only on the host and propagate the result to all clients.

#### Scenario: Both players reach doors
- **WHEN** both Fireboy and Watergirl are on their respective doors
- **THEN** the host triggers the win sequence and all clients see the win animation and results screen

### Requirement: Networked Lose Condition (Death)
The system SHALL synchronize player death as a game-over event triggered by the host.

#### Scenario: Player dies in hazard
- **WHEN** a player character contacts a fatal liquid (wrong element or green goo)
- **THEN** the host sets the game state to "lost" and all clients see the death animation followed by the game-over screen

### Requirement: Networked Scene Loading
The system SHALL use Fusion's NetworkSceneManager so the host controls level transitions and all clients load the same scene synchronously.

#### Scenario: Host selects level
- **WHEN** the host selects a level from the level selection UI
- **THEN** all connected clients load the same level simultaneously via Fusion's scene management

#### Scenario: Level transition after win
- **WHEN** a level is completed successfully
- **THEN** the host triggers the next scene transition and all clients follow

### Requirement: Multiplayer Pause
The system SHALL NOT use `Time.timeScale = 0` for pausing in online multiplayer mode. Instead, the system SHALL freeze local input and show a pause overlay without stopping the Fusion network tick.

#### Scenario: Player pauses in online mode
- **WHEN** a player presses the pause button in an online game
- **THEN** their local input is suppressed and a pause overlay is displayed, but the network tick continues for the other player

#### Scenario: Both players pause
- **WHEN** both players have their pause overlay active simultaneously
- **THEN** neither player sends input and the game effectively pauses (no state changes occur)

### Requirement: Mid-Game Disconnect Handling
The system SHALL handle player disconnection during active gameplay gracefully.

#### Scenario: Host disconnects during gameplay
- **WHEN** the host's connection is lost during a level
- **THEN** the client sees a "Host disconnected" message and is returned to the main menu

#### Scenario: Client disconnects during gameplay
- **WHEN** the client's connection is lost during a level
- **THEN** the host sees a "Player disconnected" message and is returned to the main menu
