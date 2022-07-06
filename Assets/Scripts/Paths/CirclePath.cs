using UnityEngine;

public class CirclePath : Path {
  [SerializeField]
  Color Color = Color.green;
  [SerializeField]
  [Range(0, 100)]
  float Radius = 1f;
  [SerializeField]

  public override PathData ToWorldSpace(float interpolant) {
    var axialRotation = Quaternion.AngleAxis(interpolant*360, transform.up);
    var position = transform.position+axialRotation*transform.forward*Radius;
    var tangent = axialRotation*transform.right;
    var rotation = Quaternion.LookRotation(tangent, transform.up);
    return new PathData(position, rotation);
  }

  void OnDrawGizmos() {
    Gizmos.color = Color;
    Gizmos.DrawWireSphere(transform.position, Radius);
    Gizmos.DrawRay(transform.position, transform.forward*Radius);
    Gizmos.DrawRay(transform.position, transform.right*Radius);
    Gizmos.DrawRay(transform.position, -transform.forward*Radius);
    Gizmos.DrawRay(transform.position, -transform.right*Radius);
  }
}