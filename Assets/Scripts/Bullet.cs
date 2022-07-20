using UnityEngine;

public class Bullet : MonoBehaviour {
  public enum BulletType { STUN, NET }
  public BulletType Type;
  public Vector3 Direction;
  public float Speed = 5;

  Hitbox Hitbox;
  Rigidbody Body;

  public void Awake() {
    Body = GetComponent<Rigidbody>();
    Hitbox = GetComponent<Hitbox>();
  }

  public static Bullet Fire(Bullet prefab, Vector3 position, Vector3 direction, Attack attack) {
    var bullet = Instantiate(prefab, position, Quaternion.FromToRotation(Vector3.forward, direction));
    bullet.Hitbox.Attack = attack;
    bullet.Direction = direction;
    return bullet;
  }

  // TODO: remove
  public static void Fire(Bullet prefab, Vector3 position, Vector3 direction, BulletType type, float speed = 5) {
    var bullet = Fire(prefab, position, direction, null);
    bullet.Type = type;
    bullet.Speed = speed;
  }

  void FixedUpdate() {
    Body.MovePosition(transform.position + Speed * Time.deltaTime * Direction);
  }

  void OnTriggerEnter(Collider collider) {
    if (collider.gameObject.tag == "Ground")
      return;
    Destroy(gameObject, .01f);
  }
}
