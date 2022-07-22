using UnityEngine;

public class AbilityManBullet : MonoBehaviour {
  public GameObject Explosion;

  void OnCollisionEnter(Collision collision) {
    Destroy(Instantiate(Explosion, transform.position, transform.rotation), 3);
    Destroy(gameObject);
  }
}