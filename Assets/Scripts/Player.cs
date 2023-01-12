using UnityEngine;

public class Player : MonoBehaviour {
  public static Player Get() => FindObjectOfType<Player>();
  public bool Dead = false;

  void OnDeath(Vector3 normal) {
    Dead = true;
  }
}