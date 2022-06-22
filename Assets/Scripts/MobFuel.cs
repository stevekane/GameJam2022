using UnityEngine;

public class MobFuel : Mob {
  public GameObject ExplosionPrefab;

  public override void Die() {
    Instantiate(ExplosionPrefab, transform.position, Quaternion.identity);
    Destroy(gameObject, .01f);
  }
}
