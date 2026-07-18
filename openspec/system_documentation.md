# Fireboy & Watergirl: Systems Documentation

This document serves as the central reference for the newly implemented interactive physics systems and the modified player behaviors in the project. It outlines the core logic of each component and provides a clear list of Acceptance Criteria to verify their functionality.

---

## 1. Player Physics & Behavior Modifications

Several core changes were made to the player's physics and health scripts to ensure smooth interaction with slopes, liquids, and solid interactive objects.

### Modifications
* **Slope Stability (`StandardPlayerMovement.cs`)**: The velocity-based grounding check was replaced with a robust `jumpTimer` system. This prevents the physics engine from falsely interpreting slope bounces as jumps, eliminating the "slope launching" bug.
* **Anti-Sliding Logic (`StandardPlayerMovement.cs`)**: When the player is grounded on a flat surface, their upward vertical velocity is clamped (`Mathf.Min(targetY, 0f)`). This prevents the player from inadvertently sliding or "flying" up slanted kinematic solid objects (like lever handles).
* **Intent-Based Interaction (`StandardPlayerMovement.cs`)**: Exposed the `GetHorizontalInput()` method. Interactive objects can now read this to distinguish whether the player is actively pushing (pressing keys) or simply standing still on top of them.
* **Spatial Death Filtering (`PlayerHealth.cs`)**: The death trigger logic was updated to use `hazardCollider.ClosestPoint()`. The player will no longer die if they jump and hit their head on the bottom of a lava grid; death only occurs if their feet or lower body enter the lava.

---

## 2. Lever Switch System (`LeverSwitch.cs`)

The Lever system was entirely rewritten to function as a **Solid Kinematic** physics object rather than a simple trigger, providing realistic weight and feedback.

### Key Behaviors
* **Physical Resistance**: The handle is a solid kinematic object. It physically blocks the player's path.
* **Intentional Pushing**: The lever only rotates if the player physically collides with the correct side **AND** is actively holding down the movement key (Input). Standing on top of the lever without input will not rotate it.
* **Midpoint Snapping**: If the player stops pushing before the lever crosses the 50% midpoint, a background physics loop automatically pulls it back to its original resting state.
* **Jitter-Free Rotation**: Uses `rb.MoveRotation()` in `FixedUpdate` instead of `transform.localRotation`. This perfectly syncs with the player's dynamic Rigidbody, preventing visual stuttering and physics glitches.
* **Smart Initialization**: The script reads its initial rotation in the Scene View (e.g., 45° or 135°) to automatically determine its starting state and immediately synchronizes connected gates without visual popping.

---

## 3. Platform Button System (`ButtonSwitch.cs`)

The Button system was designed to perfectly mimic the original game's feel, utilizing a "virtual hitbox" for flawless detection.

### Key Behaviors
* **Solid Foundation**: The button uses a solid `BoxCollider2D`. The player physically stands on it rather than passing through it.
* **Virtual Hitbox Detection**: Instead of relying on buggy `OnCollisionEnter/Exit` events, the script uses `Physics2D.OverlapBoxAll` in `FixedUpdate` to constantly scan a small area just above the button. If a valid entity (Player, Rock) enters this box, the button registers as pressed.
* **Physical Sinking**: When pressed, the button's `localPosition` smoothly interpolates downwards. Because the player is standing on its solid collider, the player naturally sinks with the button until they hit the yellow base.

---

## 4. Gate & Moving Platform System (`Gate.cs`)

A single, unified script powers both vertical gates and horizontal/vertical moving platforms (`System_Platform`).

### Key Behaviors
* **Universal Movement**: The script smoothly interpolates an object between a `Closed Point` and an `Open Point`.
* **Smart Physics Detection**: The script automatically checks if the object has a `Rigidbody2D`. If it does (like `Gate_3`), it uses `rb.MovePosition()` at the physics framerate. This ensures that a player standing on the moving platform moves perfectly with it and does not slide off.
* **OR Logic (Reference Counting)**: The script maintains an `openSignals` counter. If multiple buttons or levers are connected to the same gate:
  * Pressing Button 1 opens the gate.
  * Pressing Button 2 adds to the open signal (gate stays open).
  * Releasing Button 1 keeps the gate open (because Button 2 is still pressed).
  * The gate will only close when **all** controlling inputs are released (count reaches 0).

---

## 5. Acceptance Criteria

Use the following checklist to verify that all systems are functioning as intended:

### Player Movement
- [ ] Running from flat ground onto an inclined slope does **not** launch the player into the air.
- [ ] Jumping and hitting the underside (ceiling) of a Lava/Water grid does **not** kill the player.

### Lever Switch
- [ ] The player cannot walk through the lever handle; it blocks movement.
- [ ] Standing on top of a slanted lever handle and pressing nothing does **not** cause the lever to rotate.
- [ ] Pushing the lever but letting go before it reaches the halfway point causes it to smoothly fall back to its original side.
- [ ] Pushing the lever past the halfway point causes it to snap to the other side and successfully opens/closes the connected gate.
- [ ] The pushing animation is completely smooth and free of jitter.

### Button Switch
- [ ] Stepping onto the blue button causes it to visually sink downwards.
- [ ] The player sinks smoothly along with the button.
- [ ] Jumping repeatedly while standing on the button does **not** cause the connected gate to flicker or close prematurely.
- [ ] Stepping off the button causes it to pop back up and closes the gate immediately.

### Co-op Gates & Platforms
- [ ] A player standing on a moving platform (`Gate_3`) moves perfectly in sync with it and does not slide off.
- [ ] If Button 1 and Button 2 are connected to the same Gate:
  - Stepping on Button 1 opens it.
  - While Button 1 is pressed, stepping on Button 2 keeps it open.
  - Stepping off Button 1 (while Button 2 is still pressed) keeps it open.
  - Stepping off both buttons closes it.
