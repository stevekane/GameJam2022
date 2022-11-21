using System.Collections;
using UnityEngine;
using static Fiber;

public class WindRun : Ability {
  public Timeval SlowDuration = Timeval.FromMillis(2000);
  public Timeval Duration = Timeval.FromMillis(3000);
  public LayerMask LayerMask;
  public QueryTriggerInteraction TriggerInteraction;
  public float Radius;

  WindRunBoostEffect WindRunBoostEffect;
  Collider[] Colliders { get => PhysicsQuery.Colliders; }
  Vector3 Position { get => AbilityManager.transform.position; }
  Status TargetStatus(GameObject g) => g.GetComponent<Hurtbox>()?.Defender.GetComponent<Status>();

  IEnumerator SlowNearby() {
    while (true) {
      var hits = Physics.OverlapSphereNonAlloc(Position, Radius, Colliders, LayerMask, TriggerInteraction);
      for (var i = 0; i < hits; i++) {
        TargetStatus(Colliders[i].gameObject)?.Add(new WindRunSlowEffect(SlowDuration.Ticks));
      }
      yield return null;
    }
  }

  public IEnumerator MakeRoutine() {
    WindRunBoostEffect = new WindRunBoostEffect();
    Status.Add(WindRunBoostEffect);
    yield return Any(SlowNearby(), Wait(Duration.Ticks));
  }

  public override void OnStop() {
    Status.Remove(WindRunBoostEffect);
  }
}