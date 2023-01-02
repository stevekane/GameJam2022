using UnityEngine;

namespace Traditional {
  public class MoveDelta : Attribute<Vector3> {
    public override Vector3 Base { get; set; } = Vector3.zero;
    public override Vector3 Evaluate(Vector3 t) {
      return t;
    }
  }
}