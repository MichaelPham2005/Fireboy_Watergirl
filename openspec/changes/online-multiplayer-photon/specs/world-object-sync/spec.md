## ADDED Requirements

### Requirement: Synchronize Door State
The system SHALL synchronize door open/close state across the network. The host SHALL evaluate door triggers authoritatively.

#### Scenario: Player enters door trigger
- **WHEN** a player character enters a door's trigger zone
- **THEN** the door opens visually on both clients and the host registers the player as ready

#### Scenario: Player leaves door trigger
- **WHEN** a player character exits a door's trigger zone
- **THEN** the door closes visually on both clients and the host unregisters the player

### Requirement: Synchronize Gem Collection
The system SHALL synchronize gem collection authoritatively through the host to prevent double-collection.

#### Scenario: Player collects a gem
- **WHEN** a player character collides with an active gem
- **THEN** the host validates the collection, disables the gem NetworkObject on all clients, and increments the networked score

#### Scenario: Simultaneous collection attempt
- **WHEN** both players trigger the same gem at nearly the same time
- **THEN** only one collection event is processed by the host and the gem is collected exactly once

### Requirement: Synchronize Pulley System
The system SHALL synchronize pulley platform positions authoritatively from the host, with smooth interpolation on the client.

#### Scenario: Player stands on pulley platform
- **WHEN** a player character stands on one side of a pulley system
- **THEN** the host calculates weight distribution and moves both platforms accordingly, with the client seeing smooth interpolated positions

#### Scenario: Rock placed on pulley platform
- **WHEN** a pushable rock lands on a pulley platform
- **THEN** the host includes the rock's weight in the pulley calculation and both platforms adjust for both clients

#### Scenario: Weight changes dynamically
- **WHEN** a player or rock leaves a pulley platform
- **THEN** the host recalculates weight and both platforms re-adjust, visible to all clients

### Requirement: Synchronize Pushable Rocks
The system SHALL synchronize pushable rock physics authoritatively from the host.

#### Scenario: Player pushes a rock
- **WHEN** a player character pushes a rock
- **THEN** the host applies the physics force and the rock's position and rotation are synchronized to the client

#### Scenario: Both players push the same rock
- **WHEN** both players push the same rock from different directions simultaneously
- **THEN** the host resolves the combined forces and the rock moves consistently for both clients

### Requirement: Synchronize Liquid Hazards (State Only)
The system SHALL ensure liquid hazard positions are consistent across clients. Liquid hazards are static level geometry and do not require active state synchronization — their positions are determined by the shared scene.

#### Scenario: Level loads with hazards
- **WHEN** both clients load the same level
- **THEN** all liquid hazards (lava, water, green goo) are in identical positions determined by the scene data
