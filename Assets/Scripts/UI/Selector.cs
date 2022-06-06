using UnityEngine;

public class Selector : MonoBehaviour {
  public float MovementRate = .0001f;
  public Targetable Target;

  void FixedUpdate() {
    if (Target) {
      var dest = Target.transform.position + Target.Height * Vector3.up;
      var curr = transform.position;
      var t = Mathf.Exp(MovementRate);
      transform.position = Vector3.Lerp(dest,curr,t);
    }
  }
}