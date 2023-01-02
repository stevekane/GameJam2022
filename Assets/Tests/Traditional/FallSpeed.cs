using UnityEngine;

namespace Traditional {
  public class FallSpeed : Attribute<float> {
    public override float Base { get; set; } = 0;
    public override float Evaluate(float t) {
      return t;
    }
  }
}