using UnityEngine;

namespace Traditional {
  public class Gravity : Attribute<float> {
    [field:SerializeField]
    public override float Base { get; set; } = -10;
    public override float Evaluate(float t) {
      return t;
    }
  }
}