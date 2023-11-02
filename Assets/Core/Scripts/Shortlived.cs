using UnityEngine;

public class Shortlived : MonoBehaviour {
  public float LifeTime = 1;
  void Start() => Destroy(gameObject, LifeTime);
}