## ADDED Requirements

### Requirement: Host Session
The system SHALL allow a player to create a new game session as host and display a short alphanumeric room code.

#### Scenario: Hosting a game
- **WHEN** a player clicks "Host Game" on the lobby screen
- **THEN** a new Fusion session is created in Host mode and a randomly generated room code (e.g., "ABCD") is displayed to the host

#### Scenario: Waiting for player
- **WHEN** the host has created a session and no client has joined yet
- **THEN** the host sees a waiting screen with the room code and a "Waiting for player..." message

### Requirement: Join Session
The system SHALL allow a player to join an existing session by entering the host's room code.

#### Scenario: Joining with valid code
- **WHEN** a player enters a valid room code and clicks "Join Game"
- **THEN** they connect to the host's session as a Client and both players transition to level selection

#### Scenario: Joining with invalid code
- **WHEN** a player enters a room code that does not match any active session
- **THEN** the system displays an error message and the player remains on the join screen

### Requirement: Player Assignment
The system SHALL assign the host as Fireboy and the joining client as Watergirl automatically.

#### Scenario: Character assignment on connection
- **WHEN** both players are connected to the session
- **THEN** the host controls Fireboy and the client controls Watergirl

### Requirement: Lobby Disconnect Handling
The system SHALL handle disconnection gracefully during the lobby phase.

#### Scenario: Host cancels session
- **WHEN** the host closes or leaves the lobby before a client joins
- **THEN** the session is destroyed and the host returns to the main menu

#### Scenario: Client disconnects from lobby
- **WHEN** the client disconnects during the lobby phase
- **THEN** the host returns to the waiting state and can accept a new client
