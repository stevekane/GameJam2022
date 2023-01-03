using UnityEngine;

namespace Traditional {
  public class Gravity : AttributeFloat {
    [field:SerializeField]
    public override float Base { get; set; } = -10;
  }
}