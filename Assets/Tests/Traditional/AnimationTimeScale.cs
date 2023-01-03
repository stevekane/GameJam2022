using UnityEngine;

namespace Traditional {
  public class AnimationTimeScale : Attribute<float> {
    [field:SerializeField]
    public override float Base { get; set; } = 1;
    public override float Evaluate(float t) {
      return t;
    }
  }
}