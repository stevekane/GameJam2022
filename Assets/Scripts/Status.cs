using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class StatusEffect {
  public enum Types { None, Knockback }
  public Types Type;

  public abstract void Reset(StatusEffect e);
  public abstract bool Apply(Rigidbody body);
}

public class KnockbackEffect : StatusEffect {
  static readonly float DRAG = 5f;
  static readonly float DONE_SPEED = .25f;

  public Vector3 Velocity;
  public KnockbackEffect(Vector3 velocity) {
    Type = Types.Knockback;
    Velocity = velocity;
  }

  public override void Reset(StatusEffect e) {
    Velocity += ((KnockbackEffect)e).Velocity;
  }
  public override bool Apply(Rigidbody body) {
    Velocity = Velocity * Mathf.Exp(-Time.fixedDeltaTime * DRAG);
    body.MovePosition(body.position + Velocity*Time.fixedDeltaTime);
    return Velocity.sqrMagnitude < DONE_SPEED*DONE_SPEED;
  }
}

public class Status : MonoBehaviour {
  public List<StatusEffect> Active = new List<StatusEffect>();
  public StatusEffect CurrentEffect { get { return Active.Count > 0 ? Active[0] : null; } }
  public StatusEffect.Types Current { get { return CurrentEffect != null ? CurrentEffect.Type : StatusEffect.Types.None; } }

  Rigidbody Body;

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
    Body = GetComponent<Rigidbody>();
  }

  private void FixedUpdate() {
    if (CurrentEffect != null && CurrentEffect.Apply(Body)) {
      Active.RemoveAt(0);
    }
  }
}
