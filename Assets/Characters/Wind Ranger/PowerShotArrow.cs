using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class PowerShotArrow : MonoBehaviour {
  public float LifeTime = 3;
  public float LaunchSpeed = 120;

  void Start() {
    Destroy(gameObject, LifeTime);
    GetComponent<Rigidbody>().AddForce(LaunchSpeed*transform.forward, ForceMode.Impulse);
  }

  void OnTriggerEnter(Collider c) {
    Debug.Log($"Arrow hit {c.name}");
  }
}