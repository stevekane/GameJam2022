using System;
using UnityEngine;

public class Bullet : MonoBehaviour {
  public enum BulletType { STUN, NET }
  public BulletType Type;
  public Vector3 Direction;
  public float Speed = 5;
  Action<Bullet, Defender> OnHit;

  Hitbox Hitbox;
  Rigidbody Body;

  public void Awake() {
    Body = GetComponent<Rigidbody>();
    Hitbox = GetComponent<Hitbox>();
  }

  public static Bullet Fire(Bullet prefab, Vector3 position, Vector3 direction, Action<Bullet, Defender> onHit) {
    var bullet = Instantiate(prefab, position, Quaternion.FromToRotation(Vector3.forward, direction));
    bullet.OnHit = onHit;
    bullet.Direction = direction;
    return bullet;
  }

  // TODO: remove
  public static Bullet Fire(Bullet prefab, Vector3 position, Vector3 direction, Attack attack = null) {
    var bullet = Instantiate(prefab, position, Quaternion.FromToRotation(Vector3.forward, direction));
    bullet.Hitbox.Attack = attack;
    bullet.Direction = direction;
    return bullet;
  }

  // TODO: remove
  public static void Fire(Bullet prefab, Vector3 position, Vector3 direction, BulletType type, float speed = 5) {
    var bullet = Fire(prefab, position, direction);
    bullet.Type = type;
    bullet.Speed = speed;
  }

  void FixedUpdate() {
    Body.MovePosition(transform.position + Speed * Time.deltaTime * Direction);
  }

  void OnTriggerEnter(Collider collider) {
    if (collider.gameObject.tag == "Ground")
      return;
    if (collider.TryGetComponent(out Hurtbox hurtbox)) {
      OnHit?.Invoke(this, hurtbox.Defender);
    }
    Destroy(gameObject, .01f);
  }
}
