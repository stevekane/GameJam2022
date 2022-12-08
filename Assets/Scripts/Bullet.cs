using UnityEngine;

public class Bullet : MonoBehaviour {
  public enum BulletType { STUN, NET }
  [SerializeField] Attributes Attributes;
  [SerializeField] HitConfig HitConfig;
  [SerializeField] BulletType Type;
  [SerializeField] Vector3 Direction;
  [SerializeField] float Speed = 5;
  [SerializeField] Rigidbody Body;

  int CollisionLayer;

  public void Awake() {
    Body = GetComponent<Rigidbody>();
  }

  public static Bullet Fire(Bullet prefab, Vector3 position, Vector3 direction, int layer) {
    var bullet = Instantiate(prefab, position, Quaternion.FromToRotation(Vector3.forward, direction));
    bullet.gameObject.SetActive(true);
    bullet.Direction = direction;
    bullet.CollisionLayer = layer;
    return bullet;
  }

  void FixedUpdate() {
    Body.MovePosition(transform.position + Speed * Time.deltaTime * Direction);
  }

  void OnTriggerEnter(Collider collider) {
    if (collider.gameObject.tag == "Ground")
      return;
    if (collider.TryGetComponent(out Hurtbox hurtbox)) {
      if (Physics.GetIgnoreLayerCollision(CollisionLayer, hurtbox.gameObject.layer))
        return;  // TODO: I forgot the point of this
      hurtbox.TryAttack(new HitParams(HitConfig, Attributes.serialized, Attributes.gameObject, gameObject));
    }
    Destroy(gameObject, .01f);
  }
}