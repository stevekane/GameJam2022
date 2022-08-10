using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour {
  public float InitialSpeed = 10;
  void Start() => GetComponent<Rigidbody>().AddForce(InitialSpeed*transform.forward, ForceMode.Impulse);
}