using UnityEngine;

namespace Traditional {
  public class MaxFallSpeed : AttributeFloat {
    [field:SerializeField]
    public override float Base { get; set; } = -10;
  }
}