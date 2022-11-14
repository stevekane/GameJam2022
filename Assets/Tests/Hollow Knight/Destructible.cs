using UnityEngine;

public class Destructible : MonoBehaviour {
  public GameObject Debris;
  void OnTriggerEnter2D(Collider2D c) {
    Destroy(gameObject);
    Destroy(Instantiate(Debris, transform.position, transform.rotation), 5);
  }
}