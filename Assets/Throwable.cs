using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Throwable : MonoBehaviour {
  public enum ThrowableState { Idle, Airborne }
  ThrowableState State = ThrowableState.Idle;

  Mob Mob;
  Rigidbody Body;

  void Start() {
    Mob = GetComponent<Mob>();
    Body = GetComponent<Rigidbody>();
    Body.isKinematic = true;
  }

  // TODO: public bool CanGrab() { return Mob.CanGrab(); } ?

  public void Throw(Vector3 impulse) {
    if (State == ThrowableState.Airborne)
      return;  // Only throw it once.
    Destroy(GetComponent<Hittable>()); // We will handle collisions from now on.
    Body.isKinematic = false;
    Body.AddForce(impulse, ForceMode.Impulse);
    State = ThrowableState.Airborne;
  }

  void OnCollisionEnter(Collision collision) {
    if (collision.gameObject.tag == "Ground")
      return;
    if (State == ThrowableState.Airborne) {
      Debug.Log($"Throwable hit someone: {collision.gameObject}");
      Mob?.MeHitThing(collision.gameObject, 0, collision.gameObject.transform.position);
    }
  }
}
