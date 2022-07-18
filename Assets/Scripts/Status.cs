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
    status.IsAirborne = Velocity.sqrMagnitude >= AIRBORNE_SPEED*AIRBORNE_SPEED;
    status.CanMove = !status.IsAirborne;
    status.CanAttack = !status.IsAirborne;
    if (Velocity.sqrMagnitude < DONE_SPEED*DONE_SPEED)
      status.Remove(this);
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
  internal Bouncebox Bouncebox;
  internal Vibrator Vibrator;
  internal Attacker Attacker;
  public AttackConfig BounceConfig;
  public AudioSource AudioSource;

  public bool CanMove = true;
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

  private void BouncedInto(Collider other) {
    if (other.gameObject.tag == "Ground") return;

    var k = Get<KnockbackEffect>();
    if (k != null && Physics.Raycast(transform.position, k.Velocity.normalized, out var hit)) {
      AudioSource.PlayOptionalOneShot(BounceConfig.HitAudioClip);
      Vibrator.Vibrate(transform.right, BounceConfig.Contact.Frames, .15f);
      VFXManager.Instance?.TrySpawnEffect(MainCamera.Instance, BounceConfig.ContactEffect, hit.point);
      var bounceVel = Vector3.Reflect(k.Velocity, hit.normal.XZ());
      Remove(k);
      Add(new HitStunEffect(BounceConfig.Contact.Frames, new KnockbackEffect(bounceVel)));
    }
  }

  private void Awake() {
    Controller = GetComponent<CharacterController>();
    Vibrator = GetComponent<Vibrator>();
    Attacker = GetComponentInChildren<Attacker>();
    Bouncebox = GetComponentInChildren<Bouncebox>();
    Bouncebox.OnHit = BouncedInto;
  }

  private void FixedUpdate() {
    CanMove = true;
    CanAttack = true;
    IsAirborne = false;
    IsHitstun = false;

    Added.ForEach(e => Active.Add(e));
    Added.Clear();
    Removed.ForEach(e => Active.Remove(e));
    Removed.Clear();

    Active.ForEach(e => e.Apply(this));

    Bouncebox.Collider.enabled = IsAirborne;
  }
}