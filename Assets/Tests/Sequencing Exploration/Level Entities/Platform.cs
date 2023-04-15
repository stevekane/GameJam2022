using UnityEngine;

public class Platform : MonoBehaviour {
  [SerializeField] float MoveSpeed = 10;
  [SerializeField] Vector3 Velocity;

  void FixedUpdate() {
    Velocity = MoveSpeed * new Vector3(Mathf.Sin(Time.time), 0, Mathf.Cos(Time.time));
    transform.position += Time.deltaTime * Velocity;
  }

  /*
  If you are going 100speed and you land on a platform going 0 speed you should
  continue going 100speed and your steering should need to overcome that inertia.

  If you are going 100speed and you land on a platform going 50 speed you should
  continue going 100 speed but your reference frame speed is now 50 speed so your
  steering should need to overcome only 50speed inertia.

  If you are going 100 speed and you land on a platform going -50 speed you should
  continue going 100 speed but your reference frame speed is now -50 speed and your
  steering should need to overcome 150 speed inertia.

  In other words, steering is relative to (v_body - v_reference)


  What about leaving a platform?

  You are on a platform not moving while the platform moves 50speed.
  You jump and are now moving 50 speed in a reference frame going 0 speed.
  You are riding
  */

  void OnTriggerEnter(Collider c) {
    if (c.TryGetComponent(out PhysicsMotion physicsMotion)) {
      physicsMotion.OverrideVelocityXZ(0, 0, 1);
      physicsMotion.ReferenceFrameVelocity = Velocity;
    }
  }

  void OnTriggerStay(Collider c) {
    if (c.TryGetComponent(out PhysicsMotion physicsMotion)) {
      physicsMotion.ReferenceFrameVelocity = Velocity;
    }
  }

  void OnTriggerExit(Collider c) {
    if (c.TryGetComponent(out PhysicsMotion physicsMotion)) {
      physicsMotion.AddVelocity(physicsMotion.ReferenceFrameVelocity);
      physicsMotion.ReferenceFrameVelocity = Vector3.zero;
    }
  }
}