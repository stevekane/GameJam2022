using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class StatusEffect {
  public enum Types { None, Knockback }
  public Types Type;

  public abstract void Reset(StatusEffect e);
  public abstract bool Apply(Status status);
}

public class KnockbackEffect : StatusEffect {
  static readonly float DRAG = 5f;
  static readonly float DONE_SPEED = 5f;

  public Vector3 Velocity;
  public KnockbackEffect(Vector3 velocity) {
    Type = Types.Knockback;
    Velocity = velocity;
  }

  public override void Reset(StatusEffect e) {
    Velocity += ((KnockbackEffect)e).Velocity;
  }
  public override bool Apply(Status status) {
    Velocity = Velocity * Mathf.Exp(-Time.fixedDeltaTime * DRAG);
    status.Move(Velocity*Time.fixedDeltaTime);
    return Velocity.sqrMagnitude < DONE_SPEED*DONE_SPEED;
  }
}

public class Status : MonoBehaviour {
  public List<StatusEffect> Active = new List<StatusEffect>();
  public StatusEffect CurrentEffect { get { return Active.Count > 0 ? Active[0] : null; } }
  public StatusEffect.Types Current { get { return CurrentEffect != null ? CurrentEffect.Type : StatusEffect.Types.None; } }
  CharacterController Controller;
  Bouncebox Bouncebox;

  public void Add(StatusEffect effect) {
    var count = Active.Count;
    var existing = Active.FirstOrDefault((e) => e.Type == effect.Type);
    if (existing != null) {
      existing.Reset(effect);
    } else {
      Active.Add(effect);
    }
  }

  private void Awake() {
    Controller = GetComponent<CharacterController>();
    Bouncebox = GetComponentInChildren<Bouncebox>();
    Bouncebox.OnHit = BouncedInto;
  }

  private void FixedUpdate() {
    if (CurrentEffect != null && CurrentEffect.Apply(this)) {
      Active.RemoveAt(0);
    }
    Bouncebox.Collider.enabled = Current == StatusEffect.Types.Knockback;
  }

  internal void Move(Vector3 delta) {
    if (Controller) {
      Controller.Move(delta);
    } else {
      transform.position += delta;
    }
  }

  void BouncedInto(Collider other) {
    if (other.gameObject.tag == "Ground") return;

    var k = CurrentEffect as KnockbackEffect;
    if (k != null && Physics.Raycast(transform.position, k.Velocity.normalized, out var hit)) {
      var bounceVel = Vector3.Reflect(k.Velocity, hit.normal.XZ());
      k.Velocity = bounceVel;
    }
  }
}
