using System.Threading.Tasks;
using UnityEngine;

public class ScriptWaitForTarget : ScriptTask {
  public float MaxDistance = 4f;
  public override Task Run(TaskScope scope, Transform self, Transform target) {
    return scope.Until(() => TargetInRange(self, target, MaxDistance));
  }

  bool TargetInRange(Transform self, Transform target, float range) {
    var delta = (target.position - self.position);
    return delta.y < range && delta.XZ().sqrMagnitude < range.Sqr();
  }
}
