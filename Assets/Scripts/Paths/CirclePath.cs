using UnityEngine;

public class CirclePath : Path {
  [SerializeField]
  Color Color = Color.green;
  [SerializeField]
  float Radius = 1f;
  [SerializeField]

  public override Vector3 ToWorldSpace(float interpolant) {
    var rotation = Quaternion.AngleAxis(interpolant*360,transform.up);
    return rotation*transform.forward*Radius;
  }

  void OnDrawGizmos() {
    Gizmos.color = Color;
    Gizmos.DrawWireSphere(transform.position,Radius);
    Gizmos.DrawRay(transform.position,transform.forward*Radius);
    Gizmos.DrawRay(transform.position,transform.right*Radius);
    Gizmos.DrawRay(transform.position,-transform.forward*Radius);
    Gizmos.DrawRay(transform.position,-transform.right*Radius);
  }
}