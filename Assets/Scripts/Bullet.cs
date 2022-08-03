using System;
using UnityEngine;

public class Bullet : MonoBehaviour {
  public enum BulletType { STUN, NET }
  public BulletType Type;
  public Vector3 Direction;
  public float Speed = 5;
  Action<Bullet, Hurtbox> OnHit;

  Rigidbody Body;

  public void Awake() {
    Body = GetComponent<Rigidbody>();
  }

  public static Bullet Fire(Bullet prefab, Vector3 position, Vector3 direction, Action<Bullet, Hurtbox> onHit) {
    var bullet = Instantiate(prefab, position, Quaternion.FromToRotation(Vector3.forward, direction));
    bullet.OnHit = onHit;
    bullet.Direction = direction;
    return bullet;
  }

  void FixedUpdate() {
    Body.MovePosition(transform.position + Speed * Time.deltaTime * Direction);
  }

  void OnTriggerEnter(Collider collider) {
    if (collider.gameObject.tag == "Ground")
      return;
    if (collider.TryGetComponent(out Hurtbox hurtbox)) {
      OnHit?.Invoke(this, hurtbox);
    }
    Destroy(gameObject, .01f);
  }
}
