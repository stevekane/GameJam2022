using UnityEngine;

public class Selector : MonoBehaviour {
  public float MovementRate = .0001f;
  public Transform Target;

  void FixedUpdate() {
    if (Target) {
      var dest = Target.position;
      var curr = transform.position;
      var t = Mathf.Exp(MovementRate);
      transform.position = Vector3.Lerp(dest,curr,t);
    }
  }
}