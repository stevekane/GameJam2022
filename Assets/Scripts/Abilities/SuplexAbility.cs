using System.Collections;
using System.Linq;
using UnityEngine;

public class ScriptedMovementEffect : StatusEffect {
  public override bool Merge(StatusEffect e) => false;
  public override void Apply(Status status) {
    status.HasGravity = false;
    status.MoveSpeedFactor = 0f;
    status.RotateSpeedFactor = 0f;
    status.CanAttack = false;
  }
}

public class SuplexAbility : Ability {
  public float MoveSpeed = 10f;
  public float TargetDistance = 5f;
  public float Damage = 30f;

  public IEnumerator AttackStart() {
    var targets = FindObjectsOfType<Hurtbox>();
    var best = targets.Aggregate(((Hurtbox)null, 0f), (best, next) => {
      // TODO: check IsVisibleFrom - but the mask system is garbage?
      if (next.transform.position.IsInFrontOf(transform, out float nextDot) && nextDot > best.Item2)
        return (next, nextDot);
      return best;
    });
    if (best.Item2 < .9) {
      Debug.Log($"No Targets found: {best.Item1} {best.Item2}");
      yield break;
    }
    var target = best.Item1.Defender;
    yield return MoveTo(target);
    yield return Toss(target);
    Stop();
  }
  public IEnumerator MoveTo(Defender target) {
    Status.Add(Using(new ScriptedMovementEffect()));
    while (true) {
      var delta = target.transform.position - Status.transform.position;
      if (delta.magnitude < TargetDistance)
        break;
      Status.Move(MoveSpeed * Time.fixedDeltaTime * delta.normalized);
      yield return null;
    }
  }
  public IEnumerator Toss(Defender target) {
    var targetStatus = target.GetComponent<Status>();
    targetStatus.Add(Using(new ScriptedMovementEffect()));
    var air = transform.position + new Vector3(0f, 25f, 0f) + transform.forward*3f;
    var ground = transform.position + transform.forward*10f;
    var positions = new[] { air, ground };
    for (int i = 0; i < 2; i++) {
      while (true) {
        var delta = positions[i] - Status.transform.position;
        if (delta.sqrMagnitude < 1f)
          break;
        var move = MoveSpeed * Time.fixedDeltaTime * delta.normalized;
        Status.Move(move);
        targetStatus.Move(move);
        yield return null;
      }
      if (i == 0)
        targetStatus.transform.up = -targetStatus.transform.up;
    }

    var hitParams = new HitParams {
      HitStopDuration = Timeval.FromMillis(400),
      Damage = Damage,
      KnockbackStrength = 20,
      KnockbackType = KnockBackType.Forward
    };
    // TODO: vfx/sfx
    target.OnHit(hitParams, transform);
    yield return Fiber.Wait(hitParams.HitStopDuration.Frames);
    targetStatus.transform.up = -targetStatus.transform.up;
  }
}
