using UnityEngine;

public class Player : MonoBehaviour {
  public static Player Get() => FindObjectOfType<Player>();
  public bool Dead = false;

  void OnDeath() {
    Dead = true;
  }
}