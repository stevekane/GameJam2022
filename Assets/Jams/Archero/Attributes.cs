using System;
using System.Collections.Generic;
using UnityEngine;

namespace Archero {
  [Serializable]
  public enum AttributeTag {
    Damage,
    Health,
    ExtraLives,  // 0 means no revives
    Knockback,
    KnockbackTaken,  // like Weight I guess but easier to reason about
    Weight,
    MoveSpeed,
    TurnSpeed,
    AttackSpeed,
    MaxFallSpeed,
    Gravity,
    HasGravity,
    CanAttack,
    IsHittable,
    IsDamageable,
    IsGrounded,
    IsHurt,
    IsInterruptible,

    Multishot,
    Ricochet,
    FrontArrow,
    Pierce,
    BouncyWall,
    DiagonalArrow,
    SideArrow,
    RearArrow,

    CritChance,
    CritDamage,

    Bolt,
    Freeze,
    Poison,
    Blaze,
    ElementDamage,
    ElementBurst,
    DarkTouch,
    DeathBomb,
    DeathNova,
    HolyTouch,

    InvincibleStar,
    ShieldGuard,

    StrongHeart,  // +40% heal power and heart drops
    Bloodthirst,  // 1.5% base HP on kill
    Rage,         // +1.2% damage per missing 1% HP
    Headshot,     // 12.5% chance to instagib
    DamageTaken,
    Inspire,      // Attack speed +25% on enemy death
    WaterWalk,
    WallWalk,

    GoldGain,

    // States
    LocalTimeScale
  }

  [Serializable]
  public class AttributeModifier {
    public static AttributeModifier TimesZero = new() { Mult = -1 };  // bah this won't work if there are any other mults present...
    public static AttributeModifier TimesOne = new() { Mult = 1 };
    public static AttributeModifier Plus(float n) => new() { Base = n };
    public static AttributeModifier Times(float n) => new() { Mult = n };

    public float Base = 0;
    public float Mult = 0;
    public float Apply(float baseValue) => (baseValue + Base) * (1 + Mult);
    public AttributeModifier Merge(AttributeModifier other) {
      Base += other.Base;
      Mult += other.Mult;
      return this;
    }
    public AttributeModifier Remove(AttributeModifier other) {
      Base -= other.Base;
      Mult -= other.Mult;
      return this;
    }
    public static void Add(Dictionary<AttributeTag, AttributeModifier> dict, AttributeTag attrib, AttributeModifier modifier) {
      var m = dict.GetOrAdd(attrib, () => new());
      m.Merge(modifier);
    }
    public static void Remove(Dictionary<AttributeTag, AttributeModifier> dict, AttributeTag attrib, AttributeModifier modifier) {
      var m = dict.GetValueOrDefault(attrib, null);
      m?.Remove(modifier);
    }
  }

  // Fuck you, Unity
  [Serializable]
  public class AttributeTagModifierPair {
    public SerializableEnum<AttributeTag> Attribute;
    public AttributeModifier Modifier;
  }

  public interface IAttributes {
    public float GetValue(AttributeTag attrib, float baseValue = 0f);
  }

  public class Attributes : MonoBehaviour, IAttributes {
    // These two are potentially confusing. They serve mostly as convenience. In general, the player will have a set of BaseUpgrades
    // where level 0 of each represents the initial value for his attributes. Mobs will use BaseAttributes that represent their default
    // values - only because that's an easier way to specify the values than creating a list of upgrades for each mob.
    public List<AttributeTagModifierPair> BaseAttributes;
    public List<Upgrade> BaseUpgrades;
    Dictionary<AttributeTag, AttributeModifier> BaseAttributesDict = new();
    Upgrades Upgrades;
    Status Status;
    private void Awake() {
      this.InitComponent(out Upgrades);
      this.InitComponent(out Status);
      Debug.Assert(BaseAttributes.Count == 0 || BaseUpgrades.Count == 0, "BaseUpgrades will add to BaseAttributes, you probably only want one of these");
      BaseUpgrades.ForEach(u => Upgrades.AddUpgrade(u));
      BaseAttributes.ForEach(kv => BaseAttributesDict.Add(kv.Attribute, kv.Modifier));
    }
    AttributeModifier MaybeMerge(AttributeModifier modifier, AttributeModifier toMerge) => toMerge != null ? modifier.Merge(toMerge) : modifier;
    AttributeModifier GetModifier(AttributeTag attrib) {
      AttributeModifier modifier = new();
      MaybeMerge(modifier, BaseAttributesDict.GetValueOrDefault(attrib, null));
      MaybeMerge(modifier, Upgrades.GetModifier(attrib));
      MaybeMerge(modifier, Status.GetModifier(attrib));
      return modifier;
    }
    public float GetValue(AttributeTag attrib, float baseValue = 0f) => GetModifier(attrib).Apply(baseValue);
    public Serialized SerializedCopy => new Serialized(this);

    // Serialized version of attributes is a snapshot of the current state of an entity's Attributes, including default
    // values for unset modifiers. Can be used to compute attribute values after an entity dies.
    public class Serialized : IAttributes {
      Dictionary<AttributeTag, AttributeModifier> Attributes = new();
      AttributeModifier GetModifier(AttributeTag attrib) => Attributes[attrib];

      public Serialized(Attributes attributes) {
        foreach (AttributeTag a in Enum.GetValues(typeof(AttributeTag)))
          Attributes.Add(a, attributes.GetModifier(a));
      }
      public float GetValue(AttributeTag attrib, float baseValue = 0f) => GetModifier(attrib).Apply(baseValue);
      public void AddModifier(AttributeTag attrib, AttributeModifier modifier) { Attributes[attrib].Merge(modifier); }
      public void ClearModifier(AttributeTag attrib) { Attributes[attrib] = new(); }

      // Don't use this!
      public static Serialized EmptyDontUse = new();
      Serialized() {
        foreach (AttributeTag a in Enum.GetValues(typeof(AttributeTag)))
          Attributes.Add(a, new());
      }
    }
  }
}