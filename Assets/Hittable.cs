using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hittable : MonoBehaviour {
  void OnCollisionEnter(Collision collision) {
    if (collision.gameObject.tag == "Ground")
      return;

    if (collision.gameObject.TryGetComponent(out Throwable thrown)) {
      Debug.Log($"Something hit me: {collision.gameObject}");
      GetComponent<Mob>()?.ThingHitMe(Mob.ThingHitMeType.Mob, collision.gameObject.transform.position);
    }
  }

  public void ExplosionHitMe(Explosion explosion) {
    Debug.Log($"Explosion hit me: {explosion}");
    GetComponent<Mob>()?.ThingHitMe(Mob.ThingHitMeType.Explosion, explosion.transform.position);
  }
}
