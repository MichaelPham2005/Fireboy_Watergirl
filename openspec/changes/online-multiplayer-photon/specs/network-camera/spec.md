## ADDED Requirements

### Requirement: Per-Player Camera Follow in Online Mode
The system SHALL provide a camera that follows the local player's character in online multiplayer mode with smooth movement and level-bounds clamping.

#### Scenario: Online mode camera follows local player
- **WHEN** the game is running in online multiplayer mode
- **THEN** each player's camera smoothly follows their own character (host sees Fireboy-centered view, client sees Watergirl-centered view)

#### Scenario: Camera stays within level bounds
- **WHEN** the local player's character is near the edge of the level
- **THEN** the camera clamps to the level bounds and does not show areas outside the play area

#### Scenario: Camera dead zone
- **WHEN** the local player's character makes very small movements
- **THEN** the camera does not jitter or shift until the character moves beyond a dead zone threshold

### Requirement: Static Camera Preserved for Local Co-op
The system SHALL preserve the current static camera behavior when playing in local co-op mode (both players on one screen).

#### Scenario: Local co-op camera
- **WHEN** the game is running in local co-op mode
- **THEN** the camera remains static per level, showing the full play area as it does currently
