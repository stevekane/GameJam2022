using System;
using UnityEngine;

public class Bullet : MonoBehaviour {
  public enum BulletType { STUN, NET }
  public BulletType Type;
  public Vector3 Direction;
  public float Speed = 5;
  int CollisionLayer;
  HitParams HitParams;

  Rigidbody Body;

  public void Awake() {
    Body = GetComponent<Rigidbody>();
  }

  public static Bullet Fire(Bullet prefab, Vector3 position, Vector3 direction, HitParams hitparams, int layer) {
    var bullet = Instantiate(prefab, position, Quaternion.FromToRotation(Vector3.forward, direction));
    bullet.HitParams = hitparams;
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
      hurtbox.Defender.OnHit(HitParams, transform);
    }
    Destroy(gameObject, .01f);
  }
}
