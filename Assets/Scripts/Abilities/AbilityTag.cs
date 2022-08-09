using System;

[Serializable, Flags]
public enum AbilityTag {
  MeleeAttack = (1 << 0),
  Channeled = (1 << 1),
  Invulnerable = (1 << 2),
  Active = (1 << 3),
}