using UnityEngine;

namespace Archero {
  static class ParabolicMotion {
    // Returns the initial launch velocity required to reach target from the origin for launching a projectile at a 45 degree incline.
    // At 45 degree angle, distance = v^2 / g. Solve for v = sqrt(distance*g)
    public static Vector3 CalcLaunchVelocity(Vector3 origin, Vector3 target) {
      var delta = target - origin;
      var v0 = Mathf.Sqrt(delta.magnitude * Mathf.Abs(Physics.gravity.y));
      var dir0 = Vector3.RotateTowards(delta.normalized, Vector3.up, 45f * Mathf.Deg2Rad, 0f);
      return v0 * dir0;
    }
  }
  [RequireComponent(typeof(Rigidbody))]
  public class Grenade : MonoBehaviour {
    public HitParams HitParams;
    public TriggerEvent ExplosionVolume;

    // Lobs at a 45 degree vertical angle towards the target, with enough initial force to reach target.
    public static Grenade LobAt(Grenade prefab, Vector3 position, Vector3 target, Attributes attacker, HitConfig hitConfig) {
      var grenade = Instantiate(prefab, position, Quaternion.identity);
      grenade.HitParams = new(hitConfig, attacker.SerializedCopy, attacker.gameObject, grenade.gameObject);

      var rb = grenade.GetComponent<Rigidbody>();
      rb.AddForce(ParabolicMotion.CalcLaunchVelocity(position, target), ForceMode.VelocityChange);
      return grenade;
    }

    void OnCollisionEnter(Collision collision) {
      var prefab = ExplosionVolume;
      var pos = transform.position;
      GameManager.Instance.GlobalScope.Start(async s => {
        var explosion = Instantiate(prefab, pos, Quaternion.identity);
        explosion.OnTriggerEnterSource.Listen(c => {
          HitParams.Source = null;
          if (c.gameObject.TryGetComponent(out Hurtbox hb) && hb.TryAttack(HitParams)) {
            // yay
          }
        });
        await s.Seconds(.15f);
        Destroy(explosion.gameObject);
      });
      Destroy(gameObject);
    }
  }
}