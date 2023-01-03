using UnityEngine;

namespace Traditional {
  public class AnimationTimeScale : AttributeFloat {
    [field:SerializeField]
    public override float Base { get; set; } = 1;
  }
}