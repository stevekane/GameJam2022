using UnityEngine;

namespace Archero {
  [RequireComponent(typeof(Rigidbody))]
  public class Projectile : MonoBehaviour {
    public HitParams HitParams;
    public float InitialSpeed = 10;

    static GameObject _Parent;
    static GameObject Parent => _Parent = _Parent ? _Parent : new GameObject("Projectiles");

    public static Projectile Fire(Projectile prefab, Vector3 position, Quaternion rotation, Attributes attacker, HitConfig hitConfig) {
      var p = Instantiate(prefab, position, rotation);
      p.transform.SetParent(Parent.transform, true);
      p.HitParams = new(hitConfig, attacker.SerializedCopy, attacker.gameObject, p.gameObject);
      return p;
    }
    void OnTriggerEnter(Collider other) { // MP: This seems to be called for child objects too?
      if (other.gameObject.TryGetComponent(out Hurtbox hb)) {
        hb.TryAttack(HitParams);
      }
    }
    void Start() {
      GetComponent<Rigidbody>().AddForce(InitialSpeed*transform.forward, ForceMode.Impulse);
    }
  }
}