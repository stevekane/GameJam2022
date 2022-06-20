using UnityEngine;

public class Bullet : MonoBehaviour {
  public enum BulletType { STUN, NET }
  public BulletType Type;
  public Vector3 Direction;
  public float Speed = 5;

  Rigidbody body;

  public void Awake() {
    body = GetComponent<Rigidbody>();
  }

  public static void Fire(Bullet prefab, Vector3 position, Vector3 direction, float speed = 5) {
    var bullet = Instantiate(prefab, position, Quaternion.FromToRotation(Vector3.forward, direction));
    bullet.Direction = direction;
    bullet.Speed = speed;
  }

  void FixedUpdate() {
    body.MovePosition(transform.position + Speed * Time.deltaTime * Direction);
  }

  void OnTriggerEnter(Collider collider) {
    if (collider.gameObject.tag == "Ground")
      return;

    var player = collider.gameObject.GetComponentInParent<Hero>();
    if (player) {
      if (Type == BulletType.NET) {
        player.Bump(player.transform.position, transform.forward.XZ().normalized * 15f);
        player.Stun(1.25f);
      } else {
        player.Stun(.25f);
      }
    }
    Destroy(gameObject, .01f);
  }
}
