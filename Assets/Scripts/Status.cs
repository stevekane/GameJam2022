using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public delegate void OnEffectComplete(Status status);

public abstract class StatusEffect {
  public OnEffectComplete OnComplete;
  public abstract bool Merge(StatusEffect e);
  public abstract void Apply(Status status);
}

public class KnockbackEffect : StatusEffect {
  static readonly float DRAG = 5f;
  static readonly float AIRBORNE_SPEED = 100f;
  static readonly float DONE_SPEED = 5f;

  public Vector3 Velocity;
  public KnockbackEffect(Vector3 velocity) {
    Velocity = velocity;
  }

  public override bool Merge(StatusEffect e) {
    Velocity += ((KnockbackEffect)e).Velocity;
    return true;
  }
  public override void Apply(Status status) {
    Velocity = Velocity * Mathf.Exp(-Time.fixedDeltaTime * DRAG);
    status.Move(Velocity*Time.fixedDeltaTime);
    status.IsAirborne = Velocity.sqrMagnitude >= AIRBORNE_SPEED*AIRBORNE_SPEED;
    status.CanMove = !status.IsAirborne;
    status.CanAttack = !status.IsAirborne;
    if (Velocity.sqrMagnitude < DONE_SPEED*DONE_SPEED)
      status.Remove(this);
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
    Frames = TotalFrames;
    return true;
  }

  public override void Apply(Status status) {
    if (Frames <= TotalFrames) {
      status.CanAttack = false;
      status.CanMove = false;
      status.GetComponent<Animator>()?.SetSpeed(0);
      status.GetComponent<Vibrator>()?.Vibrate(Axis, TotalFrames, Amplitude);
      Frames++;
    } else {
      status.CanAttack = true;
      status.CanMove = true;
      status.GetComponent<Animator>()?.SetSpeed(1);
      status.Remove(this);
    }
  }
}

// TODO: Move to Damage.cs?
public class HitStunEffect : StatusEffect {
  public int Frames, HitStopFrames;

  public HitStunEffect(int hitStopFrames) {
    Frames = 0;
    HitStopFrames = hitStopFrames;
  }
  public override bool Merge(StatusEffect e) {
    var h = (HitStunEffect)e;
    h.HitStopFrames = Mathf.Max(HitStopFrames, h.HitStopFrames);
    return true;
  }
  public override void Apply(Status status) {
    status.CanMove = false;
    status.CanAttack = false;
    status.IsHitstun = true;
    if (Frames++ == 0) {
      status.Attacker?.CancelAttack();
    } else if (Frames >= HitStopFrames) {
      status.Remove(this);
    }
  }
}

public class Status : MonoBehaviour {
  public List<StatusEffect> Active = new();
  internal CharacterController Controller;
  internal Attacker Attacker;

  public bool CanMove = true;
  public bool CanAttack = true;
  public bool IsAirborne = false;
  public bool IsHitstun = false;

  List<StatusEffect> Added = new();
  public void Add(StatusEffect effect, OnEffectComplete onComplete = null) {
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
    Attacker = GetComponentInChildren<Attacker>();
  }

  private void FixedUpdate() {
    CanMove = true;
    CanAttack = true;
    IsAirborne = false;
    IsHitstun = false;

    Removed.ForEach(e => { e.OnComplete?.Invoke(this); Active.Remove(e); });
    Removed.Clear();
    Added.ForEach(e => Active.Add(e));
    Added.Clear();

    Active.ForEach(e => e.Apply(this));
  }
}