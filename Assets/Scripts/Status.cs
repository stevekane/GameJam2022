using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class StatusEffect {
  public enum Types { None, Knockback }
  public abstract Types Type { get; }

  public abstract bool Reset(StatusEffect e);
  public abstract void Apply(Status status);
}

public class AutoAimEffect : StatusEffect {
  public override Types Type { get => Types.None; }
  public override bool Reset(StatusEffect e) => true;
  public override void Apply(Status status) {
    status.CanRotate = false;
  }
}

public class KnockbackEffect : StatusEffect {
  static readonly float DRAG = 5f;
  static readonly float AIRBORNE_SPEED = 100f;
  static readonly float DONE_SPEED = 5f;
  public override Types Type { get => Types.Knockback; }

  public Vector3 Velocity;
  public KnockbackEffect(Vector3 velocity) {
    Velocity = velocity;
  }

  public override bool Reset(StatusEffect e) {
    Velocity += ((KnockbackEffect)e).Velocity;
    return true;
  }
  public override void Apply(Status status) {
    Velocity = Velocity * Mathf.Exp(-Time.fixedDeltaTime * DRAG);
    status.Move(Velocity*Time.fixedDeltaTime);
    status.IsAirborne |= Velocity.sqrMagnitude >= AIRBORNE_SPEED*AIRBORNE_SPEED;
    status.CanMove = !status.IsAirborne;
    status.CanRotate = !status.IsAirborne;
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
  public StatusEffect PostEffect;
  public override Types Type { get => Types.None; }

  public HitStopEffect(Vector3 axis, float amplitude, int totalFrames, StatusEffect postEffect = null) {
    Frames = 0;
    Axis = axis;
    Amplitude = amplitude;
    TotalFrames = totalFrames;
    PostEffect = postEffect;
  }

  public override bool Reset(StatusEffect e) {
    Frames = TotalFrames;
    return true;
  }

  public override void Apply(Status status) {
    if (Frames <= TotalFrames) {
      status.CanMove = false;
      status.CanRotate = false;
      status.CanAttack = false;
      status.GetComponent<Animator>()?.SetSpeed(0);
      status.GetComponent<Vibrator>()?.Vibrate(Axis, TotalFrames, Amplitude);
      Frames++;
    } else {
      status.GetComponent<Animator>()?.SetSpeed(1);
      status.Remove(this);
      if (PostEffect != null) {
        status.Add(PostEffect);
      }
    }
  }
}

// TODO: Move to Damage.cs?
public class HitStunEffect : StatusEffect {
  public int Frames, HitStopFrames;
  public StatusEffect PostEffect;
  public override Types Type { get => Types.None; }

  public HitStunEffect(int hitStopFrames, StatusEffect postEffect) {
    Frames = 0;
    HitStopFrames = hitStopFrames;
    PostEffect = postEffect;
  }
  public override bool Reset(StatusEffect e) {
    var h = (HitStunEffect)e;
    h.HitStopFrames = Mathf.Max(HitStopFrames, h.HitStopFrames);
    h.PostEffect.Reset(h.PostEffect);
    return true;
  }
  public override void Apply(Status status) {
    status.CanMove = false;
    status.CanRotate = false;
    status.CanAttack = false;
    status.IsHitstun = true;
    if (Frames++ == 0) {
      status.Attacker?.CancelAttack();
    } else if (Frames >= HitStopFrames) {
      status.Remove(this);
      status.Add(PostEffect);
    }
  }
}

public class Status : MonoBehaviour {
  public List<StatusEffect> Active = new();
  internal CharacterController Controller;
  internal Attacker Attacker;

  public bool CanMove = true;
  public bool CanRotate = true;
  public bool CanAttack = true;
  public bool IsAirborne = false;
  public bool IsHitstun = false;

  List<StatusEffect> Added = new();
  public void Add(StatusEffect effect) {
    var count = Active.Count;
    var existing = Active.FirstOrDefault((e) => e.Type == effect.Type);
    if (existing == null || !existing.Reset(effect))
      Added.Add(effect);
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
    CanRotate = true;
    CanAttack = true;
    IsAirborne = false;
    IsHitstun = false;

    Added.ForEach(e => Active.Add(e));
    Added.Clear();
    Removed.ForEach(e => Active.Remove(e));
    Removed.Clear();

    Active.ForEach(e => e.Apply(this));
  }
}