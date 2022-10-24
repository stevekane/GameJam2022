using UnityEngine;

public class Player : MonoBehaviour {
  public static Player Get() => FindObjectOfType<Player>();
}