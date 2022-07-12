using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class StatusEffect {
  public enum Types { None, Knockback }
  public abstract Types Type { get; }

  public abstract bool Reset(StatusEffect e);
  public abstract void Apply(Status status);
}

public class KnockbackEffect : StatusEffect {
  static readonly float DRAG = 5f;
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
    status.CanMove = false;
    status.CanAttack = false;
    status.IsAirborne = true;
    Velocity = Velocity * Mathf.Exp(-Time.fixedDeltaTime * DRAG);
    status.Move(Velocity*Time.fixedDeltaTime);
    if (Velocity.sqrMagnitude < DONE_SPEED*DONE_SPEED)
      status.Remove(this);
  }
}

public class DelayedEffect : StatusEffect {
  public int WaitFrames;
  public StatusEffect Effect;
  public override Types Type { get => Types.None; }

  public DelayedEffect(int wait, StatusEffect effect) {
    WaitFrames = wait;
    Effect = effect;
  }
  public override bool Reset(StatusEffect e) {
    return false;
  }
  public override void Apply(Status status) {
    // This isn't quite right... maybe something like [StunEffect(12 frames), DelayedEffect(12 frames, Knockback))] ?
    status.CanMove = false;
    status.CanAttack = false;
    if (--WaitFrames <= 0) {
      status.Remove(this);
      status.Add(Effect);
    }
  }
}

public class Status : MonoBehaviour {
  public List<StatusEffect> Active = new();
  CharacterController Controller;
  Bouncebox Bouncebox;
  Vibrator Vibrator;
  public AttackConfig BounceConfig;
  public AudioSource AudioSource;

  public bool CanMove = true;
  public bool CanAttack = true;
  public bool IsAirborne = false;

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

  private void BouncedInto(Collider other) {
    if (other.gameObject.tag == "Ground") return;

    var k = Get<KnockbackEffect>();
    if (k != null && Physics.Raycast(transform.position, k.Velocity.normalized, out var hit)) {
      AudioSource.PlayOptionalOneShot(BounceConfig.HitAudioClip);
      Vibrator.Vibrate(transform.right, BounceConfig.Contact.Frames, .15f);
      Attacker.TrySpawnEffect(BounceConfig.ContactEffect, hit.point);
      var bounceVel = Vector3.Reflect(k.Velocity, hit.normal.XZ());
      Remove(k);
      Add(new DelayedEffect(BounceConfig.Contact.Frames, new KnockbackEffect(bounceVel)));
    }
  }

  private void Awake() {
    Controller = GetComponent<CharacterController>();
    Vibrator = GetComponent<Vibrator>();
    Bouncebox = GetComponentInChildren<Bouncebox>();
    Bouncebox.OnHit = BouncedInto;
  }

  private void FixedUpdate() {
    CanMove = true;
    CanAttack = true;
    IsAirborne = false;

    Added.ForEach(e => Active.Add(e));
    Added.Clear();
    Removed.ForEach(e => Active.Remove(e));
    Removed.Clear();

    Active.ForEach(e => e.Apply(this));

    Bouncebox.Collider.enabled = IsAirborne;
  }
}