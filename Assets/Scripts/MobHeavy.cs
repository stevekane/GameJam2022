using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobHeavy : Mob {
  public GameObject Shield;

  public override void MeHitThing(GameObject thing, MeHitThingType type, Vector3 contactPos) {
    Debug.Assert(false); // Heavy can't be thrown.
  }

  public override void ThingHitMe(GameObject thing, ThingHitMeType type, Vector3 contactPos) {
    if (IsOnShieldSide(contactPos)) {
      if (type == ThingHitMeType.Explosion) {
        Destroy(Shield.gameObject);
        Shield = null;
        return;
      }
      // TODO: proper direction
      Vector3 dir = (transform.position - contactPos).XZ().normalized;
      var body = thing.GetComponent<Rigidbody>();
      body.velocity = new Vector3(0, 0, 0);
      body.AddForce(dir * 5f, ForceMode.Impulse);
    } else {
      TakeDamage();
    }
  }

  void OnTriggerEnter(Collider other) {
    if (other.gameObject.TryGetComponent(out PlayerTrigger ptrigger) && IsOnShieldSide(ptrigger.transform.position)) {
      var player = ptrigger.Hero;
      if (player.LastPerch?.gameObject != gameObject) {
        var delta = (player.transform.position - transform.position).XZ().normalized;
        player.Block(player.transform.position, delta*1f);
      }
    }
  }

  void OnTriggerStay(Collider other) {
    OnTriggerEnter(other);
  }

  // Cheesy Debug code
#if false
  private void FixedUpdate() {
    var player = GameObject.FindObjectOfType<Player>();
    Shield.transform.localScale = new Vector3(1, 1, 1f);
    if (IsOnShieldSide(player.transform.position))
      Shield.transform.localScale = new Vector3(1, 1, 2f);
  }
#endif

  bool IsOnShieldSide(Vector3 pos) {
    return Shield && IsAngleOnShieldSide(PosToAngle(pos));
  }

  float PosToAngle(Vector3 pos) {
    Vector3 dir = (pos - transform.position).XZ().normalized;
    return Mathf.Atan2(transform.forward.x, transform.forward.z) - Mathf.Atan2(dir.x, dir.z);
  }

  // Given angle [-pi, pi], return true if the shield blocks it.
  bool IsAngleOnShieldSide(float angle) {
    // Map angle from [-pi,pi] to [0,1], and rotate by 3/8 so the rear ends up from [3/4, 1].
    angle = (angle/(2*Mathf.PI) + 1f + 3/8f) % 1f;
    return (angle < 3/4f);
  }
}
