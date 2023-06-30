using System.Threading.Tasks;
using UnityEngine;

namespace Archero {
  public class Dash : ClassicAbility {
    public HitConfig HitConfig;
    public float DashSpeed = 10f;
    public float DashDistance = 5f;

    AI AI => AbilityManager.GetComponent<AI>();
    Attributes Attributes => AbilityManager.GetComponent<Attributes>();
    Transform Target => Player.Instance.transform;

    public override async Task MainAction(TaskScope scope) {
      await scope.Seconds(.25f);
      var dir = (Target.position - AbilityManager.transform.position).normalized;
      var duration = DashDistance/DashSpeed;
      AI.Velocity = DashSpeed * dir;
      await scope.Seconds(duration);
      AI.Velocity = Vector3.zero;
      await scope.Seconds(.5f);
    }
  }
}