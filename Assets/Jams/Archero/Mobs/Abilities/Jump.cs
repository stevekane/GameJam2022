using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace Archero {
  public class Jump : ClassicAbility {
    public HitConfig HitConfig;
    public float MaxJumpDistance = 10f;

    AI AI => AbilityManager.GetComponent<AI>();
    Attributes Attributes => AbilityManager.GetComponent<Attributes>();
    Transform Transform => AbilityManager.transform;
    Transform Target => Player.Instance.transform;

    public override async Task MainAction(TaskScope scope) {
      await scope.Seconds(.25f);
      try {
        var areaMask = 1 << NavMesh.GetAreaFromName("Walkable");
        var targetPos = Transform.position + MaxJumpDistance*UnityEngine.Random.insideUnitSphere.XZ();
        if (NavMesh.SamplePosition(targetPos, out var hit, 2f, areaMask)) {
          targetPos = hit.position;
        } else {
          targetPos = Target.position;
        }
        var v0 = ParabolicMotion.CalcLaunchVelocity(Transform.position, targetPos);
        var v = v0;
        AI.Motor.ForceUnground();
        while (v.y > 0f || Transform.position.y > 0.01f) {
          AI.Velocity = v;
          v.y += Time.fixedDeltaTime * Physics.gravity.y;
          await scope.Tick();
        }
        AI.Velocity = Vector3.zero;
        await scope.Tick();
      } finally {
        AI.Velocity = Vector3.zero;
      }
    }
  }
}