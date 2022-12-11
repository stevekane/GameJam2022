# Characters

A character has the following structure:

## AttributeTags

- IsGrounded
- CanAttack

## PHYSICS EVENTS

### Effects

    OnHit/OnHurt
      Fire and forget Effects

### Vibrator

    OnHit/OnHurt
      Set own properties

### Flash

    OnHit/OnHurt
      Set own properties

### Damage

    OnHit/OnHurt
      Set own properties
      Apply HitStop/Knockback/Recoil StatusEffects

## FIXED UPDATE

### Mover

    read input and move character controller
    set Attribute.IsGrounded

### AbilityManager

    if Status.Disabled
      Interupt all interuptible abilities

    run all abilities affecting status via effects

### Attributes

    Apply all Upgrades
    Apply all Statuses
