using UnityEngine;

public class AimControl : SimpleAbilityVector3 {
  public override void OnRun() {
    var xz = Value.XZ();
    if (xz.sqrMagnitude > 0)
      SimpleAbilityManager.transform.rotation = Quaternion.LookRotation(xz.normalized, Vector3.up);
    Stop();
  }
}