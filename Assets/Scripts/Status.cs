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

public class SpeedFactorEffect : StatusEffect {
  AttributeModifier MoveSpeedModifier, TurnSpeedModifier;
  public SpeedFactorEffect(float move, float rotate) => (MoveSpeedModifier, TurnSpeedModifier) = (new() { BonusMult = move }, new() { BonusMult = rotate });
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

  public Vector3 Velocity;
  public bool IsAirborne = false;
  bool IsFirstFrame = true; // Hacky way to ensure we have a hitflinch+cancel effect when a defender is first hit
  public KnockbackEffect(Vector3 velocity) {
    Velocity = velocity;
  }
  public override bool Merge(StatusEffect e) {
    Velocity += ((KnockbackEffect)e).Velocity;
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
    status.Animator?.Value.SetBool("HitFlinch", IsAirborne);
    if (!status.CanAttack) {
      status.GetComponent<AbilityManager>()?.InterruptAbilities();
    }
    if (Velocity.sqrMagnitude < DONE_SPEED*DONE_SPEED)
      status.Remove(this);
    IsFirstFrame = false;
  }
  public override void OnRemoved(Status status) {
    status.Animator?.Value.SetBool("HitFlinch", false);
  }
}

// The hit pause that happens to attacker and defender during melee attack contact.
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
    if (Frames == 0) {
      if (status.TryGetComponent(out Vibrator v))
        v.Vibrate(Axis, TotalFrames, Amplitude);
    }
    if (Frames <= TotalFrames) {
      status.CanMove = false;
      status.CanRotate = false;
      status.CanAttack = false;
      status.Animator?.Value.SetSpeed(0);
      Frames++;
    } else {
      status.Animator?.Value.SetSpeed(1);
      status.Remove(this);
    }
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
  internal Attributes Attributes;
  internal CharacterController Controller;
  internal Optional<Animator> Animator;
  Dictionary<AttributeTag, AttributeModifier> Modifiers = new();
  static AttributeModifier Zero = new() { BonusMult = 0 };

  public bool CanMove {
    get => Attributes.GetValue(AttributeTag.MoveSpeed) > 0f;
    set {
      if (value) {
        Modifiers.Remove(AttributeTag.MoveSpeed); // reset it to x1
      } else {
        AttributeModifier.Add(Modifiers, AttributeTag.MoveSpeed, Zero);
      }
    }
  }
  public bool CanRotate {
    get => Attributes.GetValue(AttributeTag.TurnSpeed) > 0f;
    set {
      if (value) {
        Modifiers.Remove(AttributeTag.TurnSpeed); // reset it to x1
      } else {
        AttributeModifier.Add(Modifiers, AttributeTag.TurnSpeed, Zero);
      }
    }
  }
  public bool HasGravity = true;  // hmmm does this make sense?
  public bool CanAttack = true;
  public bool IsHittable = true;
  public bool IsDamageable = true;
  public AbilityTag Tags = 0;
  readonly public AbilityTag BaseTags = AbilityTag.BaseForm;  // TODO: generic class to encapsulate base/current values

  List<StatusEffect> Added = new();
  public void Add(StatusEffect effect, OnEffectComplete onComplete = null) {
    effect.Status = this;
    var count = Active.Count;
    var existing = Active.FirstOrDefault((e) => e.GetType() == effect.GetType());
    if (existing == null || !existing.Merge(effect)) {
      effect.OnComplete = onComplete;
      Added.Add(effect);
    }
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

  internal void Move(Vector3 delta) {
    if (Controller) {
      Controller.Move(delta);
    } else {
      transform.position += delta;
    }
  }

  private void Awake() {
    Attributes = this.GetOrCreateComponent<Attributes>();
    Controller = GetComponent<CharacterController>();
    Animator = GetComponent<Animator>();
  }

  private void FixedUpdate() {
    HasGravity = true;
    CanAttack = true;
    IsHittable = true;
    IsDamageable = true;
    Tags = BaseTags;

    // TODO: differentiate between cancelled and completed?
    Removed.ForEach(e => { e.OnComplete?.Invoke(this); e.OnRemoved(this); e.Status = null; Active.Remove(e); });
    Removed.Clear();
    Added.ForEach(e => Active.Add(e));
    Added.Clear();
    Modifiers.Clear();

    Active.ForEach(e => e.Apply(this));
  }
}