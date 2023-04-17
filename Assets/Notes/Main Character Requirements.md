# Main character design

This document attempts to exhaustively describe the desired behavior of the main
player-controlled character. It attempts to be thorough, detailed, and orderly such
that a programmer might use it to directly inform their choice of implementation.

## Locomotion

Locomotion refers to the basic capability of the character to move around the game world.

### Running

Running the character's primary means of moving around when on the ground.

#### Requires

- Standing on ground
- Not performing uncancellable attack
- Not stunned
- Not dead
- Not in a cutscene
- Not in synched animation
- Not dashing

#### Behavior

set the forward direction of the character to input forward.
add MOVEMENT_SPEED * Forward to VELOCITY

#### Events

OnRun cancels cancellable attacks.

### Jumping

Jumping is the character's primary means of launching themself into the air when on the ground.

#### Requires

- Standing on ground
- Not performing uncancellable attack
- Not stunned
- Not dead
- Not in a cutscene
- Not in synched animation

#### Behavior

Add JUMP_STRENGTH to character's y velocity.
Animate character to begin jump.

#### Events

OnJump cancels cancellable attacks.
OnJump cancels dash.

#### Input concerns

Jump consumes input iff it is able to be performed.

### Dashing

Dashing is the character's primary means of moving quickly and gaining a window of invulnerability.

#### Requires

- Not attached to wall
- Not performing uncancellable attack
- Not stunned
- Not dead
- Not in a cutscene
- Not in synched animation
- DASH_COOLDOWN is 0

#### Behavior

##### OnStart

dashing = true

##### FixedUpdate

apply velocity to character along the forward direction

##### OnEnd

dashing = false