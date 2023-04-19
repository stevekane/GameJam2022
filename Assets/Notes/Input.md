# Input routing overview

Input routing is the task of interpreting inputs and using them to drive Actions.

A single button on a controller may be used to do various actions.

## Super Mario Brothers

Press A when grounded => Jump
Press A when in water => Swim

Press B when has firesuit and can throw fireball => fireball
Hold B when grounded => sprint

The key thing to note here is that these actions are logically exclusive:

You will never be grounded and in water so A always has a single meaning.

Imagine then that you allowed all of these actions to be bound to different buttons:

Press X when in water => Swimup
Press Y when has firesuit and can throw fireball => fireball
Press A when grounded => Jump
Press B when in water => Swim

These actions fire when their predicates are satisfied.

Now let's consider a more complex case that is reasonably contrived:

## Conditional actions

### First scenario with four buttons

We have an action that is conditionally available. Let's add it to our original
mario setup.

a. Press A when grounded and canInteract => Interact
b. Press A when grounded => Jump
c. Press A when in water => Swim
d. Press B when can fireball => Fireball
e. Hold B when grounded => Sprint

Here, a and b can both be true at the same time. Naively, this will cause us to both
interact and jump. Let's imagine we don't want that. We have two options: encode
further clauses on the condition for jumping such that the two actions are again
exclusive:

Press A when grounded and canInteract => Interact
Press A when grounded and !canInteract => Jump

Alternatively, we can move one of these actions to its own button:

Press A when grounded => Jump
Press X when grounded and canInteract => Interact

Let's consider a more constrained case now where we only have two available buttons: A and B

### Second scenario with two buttons

We once again have this situation:

a. Press A when grounded and canInteract => Interact
b. Press A when grounded => Jump

We can again elect to add additional constraints to restore exclusivity to the clauses:

Press A when grounded and canInteract => Interact
Press A when grounded and !canInteract => Jump

Alternatively, we could try a priority system to select an action to perform:

1. Press A when grounded and canInteract => Interact
2. Press A when grounded => Jump

Here, we will select an action from the list of possible behaviors that extend from the Input
Action Press A. We walk through the list until we find one that is satisfied and we fire it.

### Allowing multiple actions to run with priority

What if we want to allow multiple abilities to run when we push a button.

1. Press A when grounded => Jump
2. Press A when grounded and empowered => GainStoneSkin

Here, if we use the system mentioned previously, we end up jumping and not gaining stone skin.

We could enhance this setup be including annotations on the clauses that determine whether
actions below them have a chance to execute:

1. PASS Press A when grounded => Jump
2. Press A when grounded and empowered => GainStoneSkin

We could alternatively annotate only the abilities that we wish to explicitly block
actions below them:

1. BLOCK Press A when grounded and canInteract => Interact
2. Press A when grounded => Jump

Logically, we could take both of these actions. However, our control scheme is limited to allowing
us to do one or the other.

### A final scenario that has more actions

× means blocks subsequent actions
↓ means allows subsequent actions

Press A
× when grounded and interact => interact
↓ when grounded => jump
↓ when in water => swim

Press B
↓ when can fireball => fireball

Hold B
↓ when grounded => sprint

## What confuses me about this

Unity and Unreal both propose InputSystems that define sets of semantic actions.
For example, they might have you define the following sort of thing:

Jump
Press Controller.A
Press Keyboard.Space

Interact
Press Controller.A
Press Keyboard.I

Swim
Press Controller.A
Press Keyboard.S

Fireball
Press Controller.B
Press Keyboard.F

Sprint
Hold Controller.B
Hold Keyboard.Shift

The keyboard associates a unique key for each action and thus handling inputs is quite easy.
However, if we naively allow the player to remap their keys we need to either enforce key/action
exclusivity or we must somehow generally handle the case when all actions are bound to the same key.

This last statement is pretty absurd but to illustrate what I mean, you could bind everything
to Keboard.Space.

Jump when press Keyboard.Space
Interact when press Keyboard.Space
Swim when press Keyboard.Space
Fireball when press Keyboard.Space
Sprint when hold Keyboard.Space

The logic above would then require you to be able to prioritize all possible actions in the game
and their fall through rules.

What then is the alternative?

## Logically disjoint actions
