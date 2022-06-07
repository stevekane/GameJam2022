using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hittable : MonoBehaviour {
  void OnCollisionEnter(Collision collision) {
    if (collision.gameObject.tag == "Ground")
      return;

    if (collision.gameObject.TryGetComponent(out Throwable thrown)) {
      Debug.Log($"Something hit me: {collision.gameObject}");
      GetComponent<Mob>()?.ThingHitMe(collision.gameObject, Mob.ThingHitMeType.Mob, collision.gameObject.transform.position);
    }
  }

  public void ExplosionHitMe(Explosion explosion) {
    Debug.Log($"Explosion hit me: {explosion}");
    GetComponent<Mob>()?.ThingHitMe(explosion.gameObject, Mob.ThingHitMeType.Explosion, explosion.transform.position);
  }
}
