using UnityEngine;
using UnityEngine.Events;

public class GrapplingHook : MonoBehaviour {
  public UnityAction<Collision> OnHit;

  void OnCollisionEnter(Collision c) {
    OnHit.Invoke(c);
  }
}