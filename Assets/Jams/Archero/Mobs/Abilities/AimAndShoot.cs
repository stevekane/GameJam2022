using System.Threading.Tasks;
using UnityEngine;

namespace Archero {
  public class AimAndShoot : ClassicAbility {
    public LayerMask EnvironmentMask;
    public LineRenderer AimLine;
    public Projectile ArrowPrefab;
    public HitConfig HitConfig;

    Attributes Attributes => AbilityManager.GetComponent<Attributes>();
    Transform Target => Player.Instance.transform;
    Vector3 Origin => transform.position + .5f*Vector3.up;
    Vector3 Dir => (Target.position - transform.position).normalized;

    public override async Task MainAction(TaskScope scope) {
      try {
        AimLine.gameObject.SetActive(true);
        await scope.Any(
          Waiter.Delay(Timeval.FromSeconds(2f)),
          Waiter.Repeat(Aim));
      } finally {
        AimLine.gameObject.SetActive(false);
      }
      Projectile.Fire(ArrowPrefab, Origin, Quaternion.LookRotation(Dir), Attributes, HitConfig);
    }

    async Task Aim(TaskScope scope) {
      var origin = Origin;
      var dir = Dir;
      transform.forward = dir;  // TODO?
      AimLine.SetPosition(0, origin);
      for (var i = 1; i < AimLine.positionCount; i++) {
        if (Physics.Raycast(origin, dir, out var hit, 10000f, EnvironmentMask)) {
          origin = hit.point;
          dir = Vector3.Reflect(dir, hit.normal);
          dir.y = 0f;
          AimLine.SetPosition(i, origin);
        }
      }
      await scope.Tick();
    }
  }
}