# Routing of intentions to outcomes for characters

I want to think through the elegant and general solution to routing intents
and conditional availabity of behaviors for characters. The goal is to enumerate
the distinct components of the system and to define how they are connected to build
complete behaviors for a character.

## Input Binding

These are processes that evaluate raw input data coming from connected devices
such as keyboard/mouse/controller. They define Interaction criteria that determine
what must occur in the input stream for a binding to "fire".

## Input Action

Input Actions are sort of like semantic capabilities of a character. They represent
behavioral concepts in your gameplay and can range from being somewhat abstract like
"confirm" to highly specific like "release grapple". Most importantly, they may be
fired by multiple possible Input Bindings. Each Input Action has a list of associated
Input Bindings which can trigger it.

### Input Actions and Bindings

Imagine you have Input Bindings b1,b2,b3 and Input Actions a1 and a2.

a1 triggered by b1 or b2
a2 triggered by b1 or b3

The input system will fire both a1 and a2 when b1 fires.
If you wish to avoid this, there are two possible ways of doing so:

1. Control which InputActions (or ActionMaps) are active in code such that only the
Input Actions you wish to be fired at that moment in gameplay are possible.
2. Only create a single InputAction for each Binding. Control what happens in a callback
function that determines which action should be taken based on the context.

Our *TEST CONFIRMED* that all InputActions which can fire will fire if they are active.
This suggests that the best way forward is to control which InputActions (or InputActionMaps)
are active and thus which will attempt to fire.

1. Update active InputActionMaps and InputActions from their Actions
2. Check if Actions have fired and fire their associated Actions

## Potential Actions

Potential Actions are capabilities or "things a character may be able to do". As such,
they are a tuple of a predicate and an action. The intention is that you check the predicate
to know if the action can be performed and then fire the action to do the work.

## Abilities

Why does an ability exist as a concept?
Abilities are standardization around scripts running on an entity.
In general, you just have scripts running on an entity that may have public methods
that may be invoked by external scripts to cause them to do things. This is truly as general
as it sounds.

For example, you might have a script that defines an "Attack" public function.
When you invoke this function, the script spawns a Task associated with a timeline asset
that allows you to run the attack until it is complete. The running task also sets a boolean
on the Attack script allowing external systems to know if an attack is currently running.

You may have various reasons for wanting to stop an Attack:

1. You take damage
2. You die
3. The round ends
4. You fall off a cliff
5. You cancel the attack with a new attack
6. You cancel the attack by jumping
7. Your attack is parried

You may also have various reasons for not allowing an attack to start:

1. You are stunned
2. You are dead
3. You are in the air
4. You have a menu open
5. You are in a town
6. You are grappling
7. You are hanging on a cliff
8. You are attached to a wall
9. You don't have enough stamina
10. You don't have a weapon equipped
11. You are in a cutscene

When we look at the existing Ability abstraction, we see that it has a few features:

1. Abilities have guarded entrypoints
2. Abilities have a stop method
3. Tagged abilities are stopped when you take damage
4. Tagged abilities may prevent other abilities from starting
5. Tagged abilities may be stopped when an ability is started

If we analyze these features a little more closely we can start to understand more of the
details of what they mean in our larger context.

Ability.Stop is a generic way to say "I want to stop this running system".
Ability EntryPoints are Potential Actions.
InputActions are associated with Potential Actions and are enabled if the potential action is permitted.