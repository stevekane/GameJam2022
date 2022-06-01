using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Knockable : MonoBehaviour {
  public enum KnockableState { Waiting, Knocked }
  public KnockableState State = KnockableState.Waiting;

  Mob Mob;
  Rigidbody Body;

  void Start() {
    Mob = GetComponent<Mob>();
    Body = GetComponent<Rigidbody>();
    Body.isKinematic = true;
  }

  public void Knock(Vector3 impulse) {
    Body.isKinematic = false;
    Body.AddForce(impulse, ForceMode.Impulse);
    State = KnockableState.Knocked;
  }

  void OnCollisionEnter(Collision collision) {
    if (State == KnockableState.Knocked) {
      // We were knocked into something.
      if (Mob && collision.gameObject.GetComponent<Knockable>() != null) {
        Debug.Log($"Knockable hit someone: {collision.gameObject}");
        Mob.TakeDamage();
      }
    } else {
      // Something knocked into us.
      if (Mob && collision.gameObject.GetComponent<Knockable>() != null) {
        Debug.Log($"Somebody hit me: {collision.gameObject}");
        Mob.TakeDamage();
      }
    }
  }
}
