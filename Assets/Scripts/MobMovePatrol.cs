using UnityEngine;

public class MobMovePatrol : MobMove {
  public Transform Target;

  Rigidbody Rigidbody;

  void Start() {
    Rigidbody = GetComponent<Rigidbody>();
  }

  void FixedUpdate() {
    Rigidbody.MovePosition(Target.position);
    Rigidbody.MoveRotation(Target.rotation);
  }

  public void OnDrawGizmos() {
    if (Target)
      Gizmos.DrawLine(Target.position,transform.position);
  }
}