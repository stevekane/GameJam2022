using UnityEngine;

public class PersonalCamera : MonoBehaviour {
  public Camera Current;

  void Start() {
    Current = Camera.main;
  }
}