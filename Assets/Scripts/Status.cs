using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

public class TimedEffect : StatusEffect {
  protected int Ticks = 0;
  protected int TotalTicks;
  public TimedEffect(int ticks) => TotalTicks = ticks;
  public override sealed void Apply(Status status) {
    if (Ticks++ < TotalTicks) {
      ApplyTimed(status);
    } else {
      status.Remove(this);
    }
  }
  public virtual void ApplyTimed(Status status) { }
  public override bool Merge(StatusEffect e) {
    var other = (TimedEffect)e;
    TotalTicks = Mathf.Max(TotalTicks - Ticks, other.TotalTicks);
    Ticks = 0;
    return true;
  }
}

public class InlineEffect : StatusEffect {
  Action<Status> ApplyFunc;
  string Name;
  public InlineEffect(Action<Status> apply, string name = "InlineEffect") => (ApplyFunc, Name) = (apply, name);
  public override bool Merge(StatusEffect e) => false;
  public override void Apply(Status status) => ApplyFunc(status);
  public override string ToString() => Name;
}

public class UninterruptibleEffect : StatusEffect {
  public override bool Merge(StatusEffect e) => false;
  public override void Apply(Status status) => status.IsInterruptible = false;
}

public class SlowFallDuration : TimedEffect {
  public AttributeModifier Modifier;
  public SlowFallDuration(int ticks) : base(ticks) => Modifier = new AttributeModifier { Mult = .5f };
  public override void ApplyTimed(Status status) => status.AddAttributeModifier(AttributeTag.Gravity, Modifier);
}

public class HitStopEffect : TimedEffect {
  public Vector3 Axis;
  public HitStopEffect(Vector3 axis, int ticks) : base(ticks) => Axis = axis;
  public override void ApplyTimed(Status status) {
    status.CanAttack = false;
    var localTimeScale = Defaults.Instance.HitStopLocalTime.Evaluate((float)Ticks/TotalTicks);
    var localTimeScaleMod = new AttributeModifier { Mult = localTimeScale };
    status.AddAttributeModifier(AttributeTag.LocalTimeScale, localTimeScaleMod);
  }
}

public class HurtStunEffect : TimedEffect {
  public HurtStunEffect(int ticks) : base(ticks) { }
  public override void ApplyTimed(Status status) {
    status.CanMove = false;
    status.CanRotate = false;
    status.CanAttack = false;
    status.IsHurt = true;
  }
}

public class BurningEffect : TimedEffect {
  float DamagePerFrame = 1f;
  public BurningEffect(float dps) : base(Timeval.FromSeconds(3f).Ticks) {
    DamagePerFrame = dps / Timeval.FixedUpdatePerSecond;
  }
  public override void ApplyTimed(Status status) {
    status.Damage?.Value.AddPoints(DamagePerFrame);
  }
}

public class FreezingEffect : TimedEffect {
  public FreezingEffect(int ticks) : base(ticks) { }
  public override void ApplyTimed(Status status) {
    status.CanMove = false;
    status.CanRotate = false;
    status.CanAttack = false;
    status.AddAttributeModifier(AttributeTag.LocalTimeScale, AttributeModifier.TimesZero);
  }
}

// The flying state that happens to a defender when they get hit by a strong attack.
public class KnockbackEffect : StatusEffect {
  const float DRAG = 5;
  const float AIRBORNE_SPEED = 100f;
  const float DONE_SPEED = 5f;

  // Returns the speed needed to cover the given distance with exponential decay due to drag.
  public static float GetSpeedToTravelDistance(float distance) => DRAG*distance;

  public float Drag;
  public Vector3 Velocity;
  public Vector3? WallbounceTarget;  // TODO: Obsolete for now. Remove at some point.
  public bool IsAirborne = false;
  public KnockbackEffect(Vector3 velocity, Vector3? wallbounceTarget = null, float drag = 5f) {
    Velocity = velocity;
    WallbounceTarget = wallbounceTarget;
    Drag = drag;
  }
  public override bool Merge(StatusEffect e) {
    Velocity = ((KnockbackEffect)e).Velocity;
    return true;
  }
  public override void Apply(Status status) {
    Velocity = Velocity * Mathf.Exp(-Time.fixedDeltaTime * Drag);
    status.Mover.Move(Velocity*Time.fixedDeltaTime);
    IsAirborne = Velocity.sqrMagnitude >= AIRBORNE_SPEED.Sqr();
    if (IsAirborne) {
      status.CanMove = false;
      status.CanRotate = false;
      status.CanAttack = false;
    }
    if (Velocity.sqrMagnitude < DONE_SPEED.Sqr())
      status.Remove(this);
  }
}

// Vault effect from grappling
public class VaultEffect : StatusEffect {
  public float Drag;
  public Vector3 Velocity;
  public VaultEffect(Vector3 velocity, float drag = 5f) {
    Velocity = velocity;
    Drag = drag;
  }
  public override bool Merge(StatusEffect e) {
    var vaultEffect = (VaultEffect)e;
    Velocity = vaultEffect.Velocity;
    Drag = vaultEffect.Drag;
    return true;
  }
  public override void Apply(Status status) {
    if (status.IsGrounded) {
      status.Remove(this);
    } else {
      Velocity = Velocity * Mathf.Exp(-Time.fixedDeltaTime * Drag);
      status.Mover.Move(Velocity*Time.fixedDeltaTime);
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
  // Ignore subsequent recoils.
  public override bool Merge(StatusEffect e) => true;
  public override void Apply(Status status) {
    Velocity = Velocity * Mathf.Exp(-Time.fixedDeltaTime * DRAG);
    status.Mover.Move(Velocity*Time.fixedDeltaTime);
    status.CanMove = false;
    status.CanRotate = false;
    if (Velocity.sqrMagnitude < DONE_SPEED.Sqr())
      status.Remove(this);
  }
}

// Some forward-translation juice for melee hits to convey momentum.
public class HitFollowthroughEffect : TimedEffect {
  Vector3 Velocity;
  List<Status> Defenders = new();
  public HitFollowthroughEffect(Vector3 velocity, Timeval duration, GameObject defender) : base(duration.Ticks) {
    Velocity = velocity;
    MaybeAddDefender(defender);
  }
  public override bool Merge(StatusEffect e) {
    ((HitFollowthroughEffect)e).Defenders.ForEach(d => MaybeAddDefender(d.gameObject));
    return true;
  }
  void MaybeAddDefender(GameObject defender) {
    if (defender && defender.TryGetComponent(out Status status) && status.IsHittable && status.IsInterruptible)
      Defenders.Add(status);
  }
  public override void ApplyTimed(Status status) {
    if (Defenders.Count == 0)
      return;
    var remaining = Defenders.Where(d => d != null);
    var delta = Velocity*Time.fixedDeltaTime;
    if (CanMove(status, delta) && remaining.Any(s => CanMove(s, delta))) {
      Move(status, delta);
    }
    remaining.ForEach(s => { if (CanMove(s, delta)) Move(s, delta); });
  }
  public bool CanMove(Status target, Vector3 delta) => !target.IsGrounded || target.IsOverGround(delta);
  public void Move(Status target, Vector3 delta) {
    target.Mover.Move(delta);
    target.CanMove = false;
    target.CanRotate = false;
  }
}

public class FallenEffect : StatusEffect {
  AnimationJobConfig RecoveryAnimation;
  public FallenEffect(AnimationJobConfig recover) => RecoveryAnimation = recover;
  public override bool Merge(StatusEffect e) => true;
  public override void Apply(Status status) {
    status.IsFallen = true;
    if (status.IsGrounded) {
      status.CanMove = false;
      status.CanRotate = false;
    }
  }

  // TODO: This doesn't feel like the right place for this...
  bool Recovering = false;
  public async Task RecoverySequence(TaskScope scope) {
    if (Recovering) return;
    try {
      Recovering = true;
      if (Status.IsGrounded)
        await Status.AnimationDriver.Play(scope, RecoveryAnimation).WaitDone(scope);
      Status.Remove(this);
    } finally {
      Recovering = false;
    }
  }
}

public class Status : MonoBehaviour {
  public List<StatusEffect> Active = new();
  internal AnimationDriver AnimationDriver;
  internal Attributes Attributes;
  internal Upgrades Upgrades;
  internal CharacterController CharacterController;
  internal Mover Mover;
  internal Optional<Damage> Damage;
  Dictionary<AttributeTag, AttributeModifier> Modifiers = new();

  public bool IsGrounded { get; private set; }
  public bool JustGrounded { get; private set; }
  public bool JustTookOff { get; private set; }
  public bool IsWallSliding { get; private set; }
  public bool IsFallen { get; set; }
  public bool IsHurt { get; set; }
  public bool CanMove { get => GetBoolean(AttributeTag.MoveSpeed); set => SetBoolean(AttributeTag.MoveSpeed, value); }
  public bool CanRotate { get => GetBoolean(AttributeTag.TurnSpeed); set => SetBoolean(AttributeTag.TurnSpeed, value); }
  public bool HasGravity { get => GetBoolean(AttributeTag.HasGravity); set => SetBoolean(AttributeTag.HasGravity, value); }
  public bool CanAttack { get => GetBoolean(AttributeTag.CanAttack); set => SetBoolean(AttributeTag.CanAttack, value); }
  public bool IsInterruptible { get => GetBoolean(AttributeTag.IsInterruptible); set => SetBoolean(AttributeTag.IsInterruptible, value); }
  public bool IsHittable { get => GetBoolean(AttributeTag.IsHittable); set => SetBoolean(AttributeTag.IsHittable, value); }
  public bool IsDamageable { get => GetBoolean(AttributeTag.IsDamageable); set => SetBoolean(AttributeTag.IsDamageable, value); }
  public AbilityTag Tags = 0;

  // All booleans default to true. Set to false after Modifiers.Clear() if you want otherwise.
  bool GetBoolean(AttributeTag attrib) => Attributes.GetValue(attrib, 1f) > 0f;
  void SetBoolean(AttributeTag attrib, bool value) {
    if (value) {
      Modifiers.Remove(attrib); // reset it to default
    } else {
      AttributeModifier.Add(Modifiers, attrib, AttributeModifier.TimesZero);
    }
  }

  List<StatusEffect> Added = new();
  public StatusEffect Add(StatusEffect effect, OnEffectComplete onComplete = null) {
    Debug.Assert(!Active.Contains(effect), $"Effect {effect} is getting reused");
    Debug.Assert(!Added.Contains(effect), $"Effect {effect} is getting reused");
    effect.Status = this;
    var count = Active.Count;
    var existing = Active.FirstOrDefault((e) => e.GetType() == effect.GetType()) ?? Added.FirstOrDefault((e) => e.GetType() == effect.GetType());
    if (existing != null && existing.Merge(effect))
      return existing;
    effect.OnComplete = onComplete;
    Added.Add(effect);
    return effect;
    // TODO: merge onComplete with existing.OnComplete?
  }

  // TODO HACK: Use double-buffering instead.
  List<Action<Status>> NextTick = new();
  public void AddNextTick(Action<Status> func) => NextTick.Add(func);

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

  private void Awake() {
    Attributes = this.GetOrCreateComponent<Attributes>();
    Upgrades = this.GetOrCreateComponent<Upgrades>();
    CharacterController = GetComponent<CharacterController>();
    Mover = GetComponent<Mover>();
    AnimationDriver = GetComponent<AnimationDriver>();
    Damage = GetComponent<Damage>();
  }

  internal bool IsOverGround(Vector3 delta) {
    const float GROUND_DISTANCE = .2f;
    var cylinderHeight = Mathf.Max(0, CharacterController.height - 2*CharacterController.radius);
    var offsetDistance = cylinderHeight / 2;
    var offset = offsetDistance*Vector3.up;
    var skinOffset = CharacterController.skinWidth*Vector3.up;
    var position = transform.TransformPoint(CharacterController.center + skinOffset - offset) + delta;
    var ray = new Ray(position, Vector3.down);
    var hit = new RaycastHit();
    var didHit = Physics.SphereCast(ray, CharacterController.radius, out hit, GROUND_DISTANCE, Defaults.Instance.EnvironmentLayerMask, QueryTriggerInteraction.Ignore);
    return didHit && hit.collider.CompareTag(Defaults.Instance.GroundTag);
  }

  private void FixedUpdate() {
    Modifiers.Clear();

    var wasGrounded = IsGrounded;
    IsGrounded = IsOverGround(Vector3.zero);
    JustGrounded = !wasGrounded && IsGrounded;
    JustTookOff = wasGrounded && !IsGrounded;
    IsWallSliding = !IsGrounded && Mover.Velocity.y < 0f && CharacterController.collisionFlags.HasFlag(CollisionFlags.CollidedSides);
    IsHurt = false;
    IsFallen = false;
    if (JustGrounded) {
      gameObject.SendMessage("OnLand", SendMessageOptions.DontRequireReceiver);
    }
    if (JustTookOff) {
      gameObject.SendMessage("OnTakeoff", SendMessageOptions.DontRequireReceiver);
    }

    Tags = Upgrades.AbilityTags;

    // TODO: differentiate between cancelled and completed?
    Removed.ForEach(e => {
      e.OnComplete?.Invoke(this);
      e.OnRemoved(this);
      e.Status = null;
      Active.Remove(e);
      Added.Remove(e);
    });
    Removed.Clear();
    Added.ForEach(e => Active.Add(e));
    Added.Clear();
    Active.ForEach(e => e.Apply(this));
    NextTick.ForEach(f => f(this));
    NextTick.Clear();

#if UNITY_EDITOR
    DebugEffects.Clear();
    Active.ForEach(e => DebugEffects.Add($"{e}"));
#endif
  }

#if UNITY_EDITOR
  public List<string> DebugEffects = new();
  void OnDrawGizmos() {
    var controller = CharacterController != null ? CharacterController : GetComponent<CharacterController>();
    var cylinderHeight = Mathf.Max(0, controller.height - 2*controller.radius);
    var offsetDistance = cylinderHeight / 2;
    var offset = offsetDistance*Vector3.up;
    var position = transform.TransformPoint(controller.center-offset);
    Gizmos.DrawWireSphere(position, controller.radius);
  }
#endif
}