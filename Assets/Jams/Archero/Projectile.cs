using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Archero {
  [RequireComponent(typeof(Rigidbody))]
  public class Projectile : MonoBehaviour {
    const float BoltDist = 10f;
    const float RicochetDist = 10f;
    public HitParams HitParams;
    public float InitialSpeed = 10;
    int Bounces = 0;
    int Ricochets = 0;

    static GameObject _Parent;
    static GameObject Parent => _Parent = _Parent ? _Parent : new GameObject("Projectiles");

    public static Projectile Fire(Projectile prefab, Vector3 position, Quaternion rotation, Attributes attacker, HitConfig hitConfig) {
      var p = Instantiate(prefab, position, rotation);
      p.transform.SetParent(Parent.transform, true);
      p.HitParams = new(hitConfig, attacker.SerializedCopy, attacker.gameObject, p.gameObject);
      return p;
    }
    void OnTriggerEnter(Collider other) { // MP: This seems to be called for child objects too?
      if (other.gameObject.TryGetComponent(out Hurtbox hb) && hb.TryAttack(HitParams)) {
        // Hit something.
        if (HitParams.AttackerAttributes.GetValue(AttributeTag.Bolt, 0) > 0) {
          var mobs = GetMobsWithin(BoltDist);
          foreach (var mob in mobs) {
            Bolt.Create(GameManager.Instance.BoltPrefab, HitParams.Defender.transform, mob);
            mob.GetComponentInChildren<Hurtbox>().TryAttack(HitParams.AddMult(-.75f));
          }
        }
        if (Ricochets < 3 && HitParams.AttackerAttributes.GetValue(AttributeTag.Ricochet, 0) > 0 && GetNearestMob() is var target && target != null) {
          var rb = GetComponent<Rigidbody>();
          var dir = (target.transform.position - transform.position).normalized;
          rb.velocity = dir * rb.velocity.magnitude;
          transform.forward = dir;
          Ricochets++;
          HitParams.HitConfig = HitParams.HitConfig.AddMult(-.3f);
        } else if (Ricochets == 0 && HitParams.AttackerAttributes.GetValue(AttributeTag.Pierce, 0) > 0) {
          HitParams.HitConfig = HitParams.HitConfig.AddMult(-1f/3f);
        } else {
          Destroy(this.gameObject);
        }
      }
    }
    void OnCollisionEnter(Collision collision) {
      if (Bounces < 2 && HitParams.AttackerAttributes.GetValue(AttributeTag.BouncyWall, 0) > 0) {
        var rb = GetComponent<Rigidbody>();
        var contact = collision.contacts[0];  // Just pick a contact.
        rb.velocity = Vector3.Reflect(-collision.relativeVelocity, contact.normal);
        transform.position += rb.velocity * Time.fixedDeltaTime;
        transform.forward = rb.velocity.normalized;
        if (Bounces == 0)
          HitParams.HitConfig = HitParams.HitConfig.AddMult(-.5f); // 50% damage after bounce
        Bounces++;
      } else {
        Destroy(this.gameObject);
      }
    }
    void Start() {
      GetComponent<Rigidbody>().AddForce(InitialSpeed*transform.forward, ForceMode.Impulse);
    }

    Transform GetNearestMob() {
      var bestDist = Mathf.Infinity;
      Mob bestMob = null;
      foreach (var mob in MobManager.Instance.Mobs) {
        var distSqr = (mob.transform.position - transform.position).sqrMagnitude;
        if (mob.gameObject != HitParams.Defender && distSqr < bestDist)
          (bestDist, bestMob) = (distSqr, mob);
      }
      return bestDist < RicochetDist.Sqr() ? bestMob.transform : null;
    }
    IEnumerable<Mob> GetMobsWithin(float distance) {
      return MobManager.Instance.Mobs.Where(mob =>
        mob.gameObject != HitParams.Defender &&
        (mob.transform.position - transform.position).sqrMagnitude < distance.Sqr());
    }
  }
}