using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public delegate void OnEffectComplete(Status status);

public abstract class StatusEffect : IDisposable {
  internal Status Status; // non-null while Added to this Status
  public OnEffectComplete OnComplete;
  public abstract bool Merge(StatusEffect e);
  public abstract void Apply(Status status);
  public virtual void OnRemoved(Status status) { }
  public void Dispose() => Status?.Remove(this);
}

public class InlineEffect : StatusEffect {
  Action<Status> ApplyFunc;
  string Name;
  public InlineEffect(Action<Status> apply, string name = "InlineEffect") => (ApplyFunc, Name) = (apply, name);
  public override bool Merge(StatusEffect e) => false;
  public override void Apply(Status status) => ApplyFunc(status);
  public override string ToString() => Name;
}

public class SpeedFactorEffect : StatusEffect {
  AttributeModifier MoveSpeedModifier, TurnSpeedModifier;
  public SpeedFactorEffect(float move, float rotate) => (MoveSpeedModifier, TurnSpeedModifier) = (new() { Mult = move }, new() { Mult = rotate });
  public override bool Merge(StatusEffect e) => false;
  public override void Apply(Status status) {
    status.AddAttributeModifier(AttributeTag.MoveSpeed, MoveSpeedModifier);
    status.AddAttributeModifier(AttributeTag.TurnSpeed, TurnSpeedModifier);
  }
}

// The flying state that happens to a defender when they get hit by a strong attack.
public class KnockbackEffect : StatusEffect {
  static readonly float DRAG = 5f;
  static readonly float AIRBORNE_SPEED = 100f;
  static readonly float DONE_SPEED = 5f;

  // Returns the speed needed to cover the given distance with exponential decay due to drag.
  public static float GetSpeedToTravelDistance(float distance) => DRAG*distance;

  public Vector3 Velocity;
  public Vector3? WallbounceTarget;
  public bool IsAirborne = false;
  bool IsFirstFrame = true; // Hacky way to ensure we have a hitflinch+cancel effect when a defender is first hit
  public KnockbackEffect(Vector3 velocity, Vector3? wallbounceTarget) {
    Velocity = velocity;
    WallbounceTarget = wallbounceTarget;
  }
  public override bool Merge(StatusEffect e) {
    Velocity = ((KnockbackEffect)e).Velocity;
    IsFirstFrame = true;
    return true;
  }
  public override void Apply(Status status) {
    Velocity = Velocity * Mathf.Exp(-Time.fixedDeltaTime * DRAG);
    status.Move(Velocity*Time.fixedDeltaTime);
    IsAirborne = IsFirstFrame || Velocity.sqrMagnitude >= AIRBORNE_SPEED*AIRBORNE_SPEED;
    status.CanMove = !IsAirborne;
    status.CanRotate = !IsAirborne;
    status.CanAttack = !IsAirborne;
    if (Velocity.sqrMagnitude < DONE_SPEED*DONE_SPEED)
      status.Remove(this);
    IsFirstFrame = false;
  }
}

public class HitStopEffect : StatusEffect {
  public Vector3 Axis;
  public float Amplitude;
  public int TotalFrames;
  public int Frames;

  public HitStopEffect(Vector3 axis, float amplitude, int totalFrames) {
    Frames = 0;
    Axis = axis;
    Amplitude = amplitude;
    TotalFrames = totalFrames;
  }
  public override bool Merge(StatusEffect e) {
    var h = (HitStopEffect)e;
    TotalFrames = Mathf.Max(TotalFrames - Frames, h.TotalFrames);
    Frames = 0;
    return true;
  }

  public override void Apply(Status status) {
    if (Frames <= TotalFrames) {
      status.CanAttack = false;
      var localTimeScale = Defaults.Instance.HitStopLocalTime.Evaluate((float)Frames/(float)TotalFrames);
      var localTimeScaleMod = new AttributeModifier { Mult = localTimeScale };
      status.AddAttributeModifier(AttributeTag.LocalTimeScale, localTimeScaleMod);
      Frames++;
    } else {
      status.Remove(this);
    }
  }
}

public class HurtStunEffect : StatusEffect {
  int TotalFrames;
  int Frames;
  public HurtStunEffect(int totalFrames) {
    TotalFrames = totalFrames;
  }
  public override bool Merge(StatusEffect e) {
    var existing = (HurtStunEffect)e;
    TotalFrames = Mathf.Max(TotalFrames-Frames, existing.TotalFrames);
    return true;
  }
  public override void Apply(Status status) {
    if (Frames <= TotalFrames) {
      status.CanMove = false;
      status.CanRotate = false;
      status.CanAttack = false;
      status.IsHurt = true;
      Frames++;
    } else {
      status.Remove(this);
    }
  }
}

public class BurningEffect : StatusEffect {
  float DamagePerFrame = 1f;
  int Frames = Timeval.FromSeconds(3f).Ticks;
  public BurningEffect(float dps) {
    DamagePerFrame = dps / Timeval.FixedUpdatePerSecond;
  }
  public override void Apply(Status status) {
    if (--Frames <= 0) {
      status.Remove(this);
    } else {
      status.Damage?.Value.AddPoints(DamagePerFrame);
    }
  }
  public override bool Merge(StatusEffect e) {
    var other = (BurningEffect)e;
    DamagePerFrame = Mathf.Max(DamagePerFrame, other.DamagePerFrame);
    Frames = Math.Max(Frames, other.Frames);
    return true;
  }
}

public class FreezingEffect : StatusEffect {
  int Frames;
  public FreezingEffect(int frames) {
    Frames = frames;
  }
  public override void Apply(Status status) {
    if (--Frames <= 0) {
      status.Remove(this);
    } else {
      status.CanMove = false;
      status.CanRotate = false;
      status.CanAttack = false;
      status.AddAttributeModifier(AttributeTag.LocalTimeScale, AttributeModifier.TimesZero);
    }
  }
  public override bool Merge(StatusEffect e) {
    var other = (FreezingEffect)e;
    Frames = Math.Max(Frames, other.Frames);
    return true;
  }
}

// The recoil effect that pushes you back when you hit something.
public class RecoilEffect : StatusEffect {
  static readonly float DRAG = 5f;
  static readonly float DONE_SPEED = 5f;

  public Vector3 Velocity;
  public RecoilEffect(Vector3 velocity) {
    Velocity = velocity;
  }
  public override bool Merge(StatusEffect e) {
    Velocity += ((RecoilEffect)e).Velocity;
    return true;
  }
  public override void Apply(Status status) {
    Velocity = Velocity * Mathf.Exp(-Time.fixedDeltaTime * DRAG);
    status.Move(Velocity*Time.fixedDeltaTime);
    status.CanMove = false;
    status.CanRotate = false;
    status.CanAttack = false;
    if (Velocity.sqrMagnitude < DONE_SPEED*DONE_SPEED)
      status.Remove(this);
  }
}

public class Status : MonoBehaviour {
  public List<StatusEffect> Active = new();
  internal AnimationDriver AnimationDriver;
  internal Attributes Attributes;
  internal Upgrades Upgrades;
  internal CharacterController Controller;
  internal Optional<Damage> Damage;
  Dictionary<AttributeTag, AttributeModifier> Modifiers = new();

  public bool IsGrounded { get => GetBoolean(AttributeTag.IsGrounded); set => SetBoolean(AttributeTag.IsGrounded, value); }
  public bool CanMove { get => GetBoolean(AttributeTag.MoveSpeed); set => SetBoolean(AttributeTag.MoveSpeed, value); }
  public bool CanRotate { get => GetBoolean(AttributeTag.TurnSpeed); set => SetBoolean(AttributeTag.TurnSpeed, value); }
  public bool HasGravity { get => GetBoolean(AttributeTag.HasGravity); set => SetBoolean(AttributeTag.HasGravity, value); }
  public bool CanAttack { get => GetBoolean(AttributeTag.CanAttack); set => SetBoolean(AttributeTag.CanAttack, value); }
  public bool IsHittable { get => GetBoolean(AttributeTag.IsHittable); set => SetBoolean(AttributeTag.IsHittable, value); }
  public bool IsDamageable { get => GetBoolean(AttributeTag.IsDamageable); set => SetBoolean(AttributeTag.IsDamageable, value); }
  public bool IsHurt { get => GetBool(AttributeTag.IsHurt); set => SetBool(AttributeTag.IsHurt, value); }
  public AbilityTag Tags = 0;

  bool GetBoolean(AttributeTag attrib) => Attributes.GetValue(attrib, 1f) > 0f;
  void SetBoolean(AttributeTag attrib, bool value) {
    if (value) {
      Modifiers.Remove(attrib); // reset it to default
    } else {
      AttributeModifier.Add(Modifiers, attrib, AttributeModifier.TimesZero);
    }
  }
  bool GetBool(AttributeTag attrib) => Attributes.GetValue(attrib) > 0f;
  void SetBool(AttributeTag attrib, bool b) => AttributeModifier.Add(Modifiers, attrib, AttributeModifier.Plus(b ? 1 : 0));

  List<StatusEffect> Added = new();
  public StatusEffect Add(StatusEffect effect, OnEffectComplete onComplete = null) {
    effect.Status = this;
    var count = Active.Count;
    var existing = Active.FirstOrDefault((e) => e.GetType() == effect.GetType());
    if (existing != null && existing.Merge(effect))
      return existing;
    effect.OnComplete = onComplete;
    Added.Add(effect);
    return effect;
    // TODO: merge onComplete with existing.OnComplete?
  }

  List<StatusEffect> Removed = new();
  public void Remove(StatusEffect effect) {
    if (effect != null)
      Removed.Add(effect);
  }

  public T Get<T>() where T : StatusEffect {
    return Active.FirstOrDefault(e => e is T) as T;
  }

  public AttributeModifier GetModifier(AttributeTag attrib) => Modifiers.GetValueOrDefault(attrib, null);
  public void AddAttributeModifier(AttributeTag attrib, AttributeModifier modifier) => AttributeModifier.Add(Modifiers, attrib, modifier);
  public void RemoveAttributeModifier(AttributeTag attrib, AttributeModifier modifier) => AttributeModifier.Remove(Modifiers, attrib, modifier);

  public void Move(Vector3 delta) {
    GetComponent<Mover>().Move(delta);
  }

  private void Awake() {
    Attributes = this.GetOrCreateComponent<Attributes>();
    Upgrades = this.GetOrCreateComponent<Upgrades>();
    Controller = GetComponent<CharacterController>();
    AnimationDriver = GetComponent<AnimationDriver>();
    Damage = GetComponent<Damage>();
  }

  private void FixedUpdate() {
    HasGravity = true;
    CanAttack = true;
    IsHittable = true;
    IsDamageable = true;
    IsHurt = false;
    Tags = Upgrades.AbilityTags;

    // TODO: differentiate between cancelled and completed?
    Removed.ForEach(e => {
      e.OnComplete?.Invoke(this);
      e.OnRemoved(this);
      e.Status = null;
      Active.Remove(e);
    });
    Removed.Clear();
    Added.ForEach(e => Active.Add(e));
    Added.Clear();
    Modifiers.Clear();
    Active.ForEach(e => e.Apply(this));

#if UNITY_EDITOR
    DebugEffects.Clear();
    Active.ForEach(e => DebugEffects.Add($"{e}"));
#endif
  }

#if UNITY_EDITOR
  public List<string> DebugEffects = new();
#endif
}