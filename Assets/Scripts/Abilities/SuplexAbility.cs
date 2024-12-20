using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class ScriptedMovementEffect : StatusEffect {
  static AttributeModifier Modifier = new() { Mult = 0f };
  public override bool Merge(StatusEffect e) => false;
  public override void Apply(Status status) {
    status.HasGravity = false;
    status.AddAttributeModifier(AttributeTag.MoveSpeed, Modifier);
    status.AddAttributeModifier(AttributeTag.TurnSpeed, Modifier);
    status.CanAttack = false;
  }
}

public class SuplexAbility : Ability {
  public float MoveSpeed = 10f;
  public float TargetDistance = 5f;
  public float HitRadius = 10f;
  public float HitCameraShakeIntensity;
  public HitConfig HitConfig;

  public override async Task MainAction(TaskScope scope) {
    var targets = FindObjectsOfType<Hurtbox>();
    var best = targets.Aggregate(((Hurtbox)null, 0f), (best, next) => {
      // TODO: check IsVisibleFrom - but the mask system is garbage?
      if (next.transform.position.IsInFrontOf(transform, out float nextDot) && nextDot > best.Item2)
        return (next, nextDot);
      return best;
    });
    if (best.Item2 < .9) {
      Debug.Log($"No Targets found: {best.Item1} {best.Item2}");
      return;
    }
    var target = best.Item1.Owner.transform;
    await MoveTo(scope, target);
    await Toss(scope, target);
  }
  async Task MoveTo(TaskScope scope, Transform target) {
    using var e = Status.Add(new ScriptedMovementEffect());
    while (true) {
      var delta = target.transform.position - Status.transform.position;
      if (delta.magnitude < TargetDistance)
        break;
      Mover.Move(MoveSpeed * Time.fixedDeltaTime * delta.normalized);
      await scope.Tick();
    }
  }
  async Task Toss(TaskScope scope, Transform target) {
    var targetStatus = target.GetComponent<Status>();
    var targetMover = target.GetComponent<Mover>();
    using var e = Status.Add(new ScriptedMovementEffect());
    var air = transform.position + new Vector3(0f, 25f, 0f) + transform.forward*3f;
    var ground = transform.position + transform.forward*10f;
    var positions = new[] { air, ground };
    for (int i = 0; i < 2; i++) {
      while (true) {
        var delta = positions[i] - Status.transform.position;
        if (delta.sqrMagnitude < 1f)
          break;
        var move = MoveSpeed * Time.fixedDeltaTime * delta.normalized;
        Mover.Move(move);
        targetMover.Move(move);
        await scope.Tick();
      }
      if (i == 0)
        targetStatus.transform.up = -targetStatus.transform.up;
    }

    CameraShaker.Instance.Shake(HitCameraShakeIntensity);
    var hits = Physics.OverlapSphereNonAlloc(target.transform.position, HitRadius, PhysicsQuery.Colliders, Layers.CollidesWith(gameObject.layer));
    PhysicsQuery.Colliders[..hits].ForEach(c => {
      if (c.TryGetComponent(out Hurtbox hurtbox))
        hurtbox.TryAttack(new HitParams(HitConfig, Attributes));
    });
    await scope.Delay(HitConfig.HitStopDuration);
    targetStatus.transform.up = -targetStatus.transform.up;
  }
}
