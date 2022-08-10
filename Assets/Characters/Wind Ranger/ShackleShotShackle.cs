using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class ShackleShotShackle : MonoBehaviour {
  void OnCollisionEnter(Collision c) {
    Destroy(gameObject);
    Debug.Log($"Shackle hit {c.collider.name}");
  }
}