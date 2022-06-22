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

  public void Hold() {
    gameObject.layer = PhysicsLayers.MobAirborne;
    if (TryGetComponent(out MobMove move)) {
      move.enabled = false;
    }
  }

  public void Throw(Vector3 impulse) {
    if (State == ThrowableState.Airborne)
      return;  // Only throw it once.
    Destroy(GetComponent<Targetable>());
    gameObject.layer = PhysicsLayers.MobAirborne;
    Body.isKinematic = false;
    Body.AddForce(impulse.XZ(), ForceMode.Impulse);
    State = ThrowableState.Airborne;
  }

  void OnCollisionEnter(Collision collision) {
    if (collision.gameObject.tag == "Ground")
      return;  // TODO: handle this via layers?
    if (State == ThrowableState.Airborne) {
      Debug.Log($"Throwable hit someone: {collision.gameObject}");
      Mob?.MeHitThing(collision.gameObject, 0, collision.gameObject.transform.position);
    }
  }
}
