using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StatusEffect {
  public enum Types { None, Knockback }
  public Types Type;
  public int Remaining;

  public virtual void Update(StatusEffect e) {
    Remaining = Mathf.Max(e.Remaining, Remaining);
  }
}

public class KnockbackEffect : StatusEffect {
  public Vector3 Velocity;
  public KnockbackEffect(Vector3 velocity, Timeval duration) {
    this.Type = Types.Knockback;
    this.Velocity = velocity;
    this.Remaining = duration.Frames;
  }

  public override void Update(StatusEffect e) {
    base.Update(e);
    this.Velocity += ((KnockbackEffect)e).Velocity;
  }
}

public class Status : MonoBehaviour {
  public List<StatusEffect> Active = new List<StatusEffect>();
  public StatusEffect CurrentEffect { get { return Active.Count > 0 ? Active[0] : null; } }
  public StatusEffect.Types Current { get { return CurrentEffect != null ? CurrentEffect.Type : StatusEffect.Types.None; } }

  public void Add(StatusEffect effect) {
    var count = Active.Count;
    var existing = Active.FirstOrDefault((e) => e.Type == effect.Type);
    if (existing != null) {
      existing.Update(effect);
    } else {
      Active.Add(effect);
    }
  }

  private void FixedUpdate() {
    Active.ForEach((e) => { e.Remaining--; });
    Active.RemoveAll((e) => e.Remaining <= 0);

    switch (Current) {
    case StatusEffect.Types.Knockback:
      var k = (KnockbackEffect)CurrentEffect;
      transform.position += k.Velocity * Time.fixedDeltaTime;
      k.Velocity *= .99f;
      break;
    }
  }
}
