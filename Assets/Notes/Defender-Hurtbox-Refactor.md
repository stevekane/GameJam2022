# Defender Hurtbox Refactor (12-7-2022)

## TODO

- Store reference instance for BigfatSlowBoom on the ability itself with HitConfig and Attributes ref
- Store reference instance for Explosion on Bombard Ability with HitConfig and Attributes ref
- Store reference instance for Bullet on PelletAbility with HitConfig and Attributes ref
- ShieldAbility should work by toggle isHittable status of Badger while enabling the shield HurtBox
- Add hit SFX/VFX to hit configs on all relevant abilities... fuck me

## Defender changes

- Defender.FixedUpdate disables Hurtboxes if !Status.IsHittable
- Defender requires Status and Damage components

## Hurtbox changes

- Hurtbox.TryAttack(Attributes attacker, HitConfig config) is called by would-be attackers
- Hurtbox uses attacker Attributes and HitConfig to compute HitParams
- Hurtbox broadcasts HitParams using SendMessage
- Hurtbox fires OnHit EventSource with HitParams
- Hurtbox.Defender removed
- Hurtbox.Owner added
- Hurtbox.OnHit added
- Hurtbox could be subclassed to use Defender's Attributes

## AttackHitBox changes

- Actions all take Hurtboxes instead of Transforms

## HitConfig changes

- Attacker onHit VFX and SFX added (back)
- HitConfig.Scale added which yields a new HitConfig with properties scaled by a float
- HitConfig.ComputeParams takes attacker attributes and hurtbox (could take defender attributes)
- Moved to Combat/HitConfig

## HitParams changes

- Attacker onHit VFX and SFX added (back)
- KnockbackVector added
- KnockbackType removed
- Moved to Combat/HitParams

## DamageInfo changes

-- Removed

## Explosion changes

-- Removed HitParams
-- Has HitConfig and reference to owner's Attributes

## TargetedStrike changes

-- Removed HitParams

## BigFatSlowBoom changes

-- Removed HitParams
-- Has HitConfig and reference to owner's Attributes