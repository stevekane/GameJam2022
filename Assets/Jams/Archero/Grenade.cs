using UnityEngine;

namespace Archero {
  [RequireComponent(typeof(Rigidbody))]
  public class Grenade : MonoBehaviour {
    public HitParams HitParams;
    public TriggerEvent ExplosionVolume;

    // Lobs at a 45 degree vertical angle towards the target, with enough initial force to reach target.
    // At 45 degree angle, distance = v^2 / g. Solve for v = sqrt(distance*g)
    public static Grenade LobAt(Grenade prefab, Vector3 position, Vector3 target, Attributes attacker, HitConfig hitConfig) {
      var grenade = Instantiate(prefab, position, Quaternion.identity);
      grenade.HitParams = new(hitConfig, attacker.SerializedCopy, attacker.gameObject, grenade.gameObject);

      var delta = target - position;
      var v0 = Mathf.Sqrt(delta.magnitude * Mathf.Abs(Physics.gravity.y));
      var dir0 = Vector3.RotateTowards(delta.normalized, Vector3.up, 45f * Mathf.Deg2Rad, 0f);
      var rb = grenade.GetComponent<Rigidbody>();
      rb.AddForce(v0 * dir0, ForceMode.VelocityChange);
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