using UnityEngine;

namespace Archero {
  [RequireComponent(typeof(Rigidbody))]
  public class Projectile : MonoBehaviour {
    public HitParams HitParams;
    public float InitialSpeed = 10;

    static Projectile Create(Projectile prefab, Vector3 position, Quaternion rotation, HitParams hitParams) {
      var p = Instantiate(prefab, position, rotation);
      p.HitParams = hitParams;
      return p;
    }
    void Start() {
      GetComponent<Rigidbody>().AddForce(InitialSpeed*transform.forward, ForceMode.Impulse);
    }
  }
}