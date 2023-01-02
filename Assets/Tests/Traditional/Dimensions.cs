using UnityEngine;

namespace Traditional {
  public class Dimensions : Attribute<Vector3> {
    [field:SerializeField]
    public override Vector3 Base { get; set; }
    public override Vector3 Evaluate(Vector3 v) {
      return v;
    }

    void OnDrawGizmosSelected() {
      var dimensions = Evaluate(Base);
      var center = dimensions.y/2 * Vector3.up + transform.position;
      Gizmos.color = Color.grey;
      Gizmos.DrawWireCube(center, dimensions);
    }
  }
}