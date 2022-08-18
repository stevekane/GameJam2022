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

public class AutoAimEffect : StatusEffect {
  public override bool Merge(StatusEffect e) => true;
  public override void Apply(Status status) {
    status.CanRotate = false;
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
    status.Animator?.SetBool("HitFlinch", IsAirborne);
    if (!status.CanAttack) {
      status.GetComponent<AbilityManager>()?.InterruptAbilities();
    }
    if (Velocity.sqrMagnitude < DONE_SPEED*DONE_SPEED)
      status.Remove(this);
    IsFirstFrame = false;
  }
  public override void OnRemoved(Status status) {
    status.Animator?.SetBool("HitFlinch", false);
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
      status.GetComponent<Vibrator>()?.Vibrate(Axis, TotalFrames, Amplitude);
    }
    if (Frames <= TotalFrames) {
      status.CanMove = false;
      status.CanRotate = false;
      status.CanAttack = false;
      status.GetComponent<Animator>()?.SetSpeed(0);
      Frames++;
    } else {
      status.GetComponent<Animator>()?.SetSpeed(1);
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
  internal CharacterController Controller;
  internal Animator Animator;

  public bool CanMove = true;
  public bool CanRotate = true;
  public bool CanAttack = true;
  public float MoveSpeedFactor = 1f;  // TODO: replaces CanMove?
  public float RotateSpeedFactor = 1f;

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

  internal void Move(Vector3 delta) {
    if (Controller) {
      Controller.Move(delta);
    } else {
      transform.position += delta;
    }
  }

  private void Awake() {
    Controller = GetComponent<CharacterController>();
    Animator = GetComponent<Animator>();
  }

  private void FixedUpdate() {
    CanMove = true;
    CanRotate = true;
    CanAttack = true;
    MoveSpeedFactor = 1f;
    RotateSpeedFactor = 1f;

    // TODO: differentiate between cancelled and completed?
    Removed.ForEach(e => { e.OnComplete?.Invoke(this); e.OnRemoved(this); e.Status = null; Active.Remove(e); });
    Removed.Clear();
    Added.ForEach(e => Active.Add(e));
    Added.Clear();

    Active.ForEach(e => e.Apply(this));
  }
}