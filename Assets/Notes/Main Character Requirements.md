# Main character requirements

## Movement

### Requirements

  Translation when MOVE
    Add to Velocity
  Rotation when ROTATE
    Add to Rotation
  Steering when STEER
    Add to Velocity
    Add to Rotation
  Directional Influence when DI
    Add to Velocity
  Synchronized Move when SYNC_MOVE
    Add to Velocity
  RootMotion when ROOT_MOTION
    Add to Velocity
    Add to Rotation
  MotionWarp when MOTION_WARP

### Examples

  Base condition
    MOVE
    ROTATE
  Hit by melee attack
    SYNC_MOVE
  Knocked back after melee attack
    DI
  Performing Melee Attack
    ROOT_MOTION
    MOTION_WARP | STEER
  Synced Finishing Move
    ROOT_MOTION
    MOTION_WARP

### Issues

Multiple systems may attempt to control one of these conditions on every frame.
As such, when assessing what should happen in a given frame, all attribute variables
must be assumed to be able to handle multiple write requests.

MOVE = true
MOVE = false
MOVE = true
↓
MOVE = false

Here is a possible implementation:

  MOVE = if MOVE then NEW_MOVE else false

The issue with this implementation is that it is lossy. We have no way of recovering
the structure that led to the current value of move. If we introduce a requirement that
the algorithm is order-free (commutative) then we can try to write a new implementation
that can be used to recover the underlying structure but also ask about results of
modifying that structure.

  FALSE_COUNT = 0
  MOVE = FALSE_COUNT <= 0

Let's imagine we have a system that writes MOVE = false and we want to ask what the value
of MOVE would be if this system's affect were removed.

Let's assume two scenarios: FALSE_COUNT = 1 and FALSE_COUNT = 2

In the first, FALSE_COUNT = 0 therefore MOVE = true
In the second, FALSE_COUNT = 1 therefore MOVE = false

On a given frame, all writers must re-apply their desired affect to MOVE.
This might be inconvenient. It might be easier to describe two moments in time,
a start and end, at which a system writes to some state.

  START FREEDOM
    MOVE = true
  STOP FREEDOM
    MOVE = false

This should have identical behavior to the alternative statement
  WHILE FREEDOM
    MOVE = true

MOVE
  STICKY_FALSE_COUNT
  FALSE_COUNT
  ENABLE
  DISABLE => STICKY_FALSE_COUNT++
  VOTE_FALSE => FALSE_COUNT++
  VALUE = (STICKY_FALSE_COUNT + FALSE_COUNT) <= 0

What if we wanted to ask what the value of MOVE would be if we removed some operation?

Let's say we want to know if we can move if our vote to prevent moving is lifted:

  MOVE_COPY = COPY(MOVE)
  MOVE_COPY.DISABLE
  MOVE_COPY.VALUE // false because FALSE_COUNT > 0

A moment in time that we might care about:

  I am running melee attack
    MOVE.DISABLE
  I want to stop running melee attack and start moving if possible
    COPY_MOVE.ENABLE
    Move if COPY_MOVE.VALUE

This system has a flaw: The order that systems run to affect MOVE will determine
what value MOVE has at the moment a particular system is running.

  Let's say we have two systems:

    FOO
    BAR

  FOO does something if the value of MOVE is true
  BAR votes the value of MOVE to false

  If FOO runs first, it will see that MOVE is true and do something
  If BAR runs first, it will set MOVE to false and FOO will see it is false and do nothing

Removing this requires letting all votes for MOVE happen FIRST before an assessment
of the value of MOVE is allowed.

  You could make a decision based on the previous/current value of MOVE but not on
  the still-computing value of move.

MOVE
  CURRENT
    STICKY_FALSE_COUNT
    FALSE_COUNT
    ENABLE
    DISABLE => STICKY_FALSE_COUNT++
    VOTE_FALSE => FALSE_COUNT++
  NEXT
    STICKY_FALSE_COUNT
    FALSE_COUNT
    ENABLE
    DISABLE => STICKY_FALSE_COUNT++
    VOTE_FALSE => FALSE_COUNT++