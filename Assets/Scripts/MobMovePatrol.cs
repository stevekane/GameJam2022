using UnityEngine;

public class MobMovePatrol : MobMove {
  public Transform Target;

  void FixedUpdate() {
    transform.SetPositionAndRotation(Target.position,Target.rotation);
  }

  public void OnDrawGizmos() {
    if (Target)
      Gizmos.DrawLine(Target.position,transform.position);
  }
}