using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RectanglePath : Path {
  [SerializeField] 
  Color Color = Color.blue;
  [SerializeField] 
  [Range(0,100)]
  float Width = 5;
  [SerializeField]
  [Range(0,100)]
  float Height = 5;

  public override PathData ToWorldSpace(float interpolant) {
    var pathLength = 2*Width+2*Height;
    var uld = Width/pathLength;
    var urd = uld+Height/pathLength;
    var lrd = urd+Width/pathLength;
    var ul = transform.position-transform.right*Width/2+transform.forward*Height/2;
    var ur = transform.position+transform.right*Width/2+transform.forward*Height/2;
    var lr = transform.position+transform.right*Width/2-transform.forward*Height/2;
    var ll = transform.position-transform.right*Width/2-transform.forward*Height/2;
    var normalizedDistances = new List<float> { 0, uld, urd, lrd, 1};
    var points = new Vector3[] {ul, ur, lr, ll};
    interpolant = interpolant%1f;
    int i = normalizedDistances.FindLastIndex((float t) => interpolant >= t);
    return Waypoints.SegmentToWorldSpace(interpolant, points[i], points[(i+1)%4], points[(i+2)%4], normalizedDistances[i], normalizedDistances[i+1], 0.9f);
  }

  void OnDrawGizmos() {
    var ul = transform.position-transform.right*Width/2+transform.forward*Height/2;
    var ur = transform.position+transform.right*Width/2+transform.forward*Height/2;
    var lr = transform.position+transform.right*Width/2-transform.forward*Height/2;
    var ll = transform.position-transform.right*Width/2-transform.forward*Height/2;
    Gizmos.color = Color;
    Gizmos.DrawLine(ul,ur);
    Gizmos.DrawLine(ur,lr);
    Gizmos.DrawLine(lr,ll);
    Gizmos.DrawLine(ll,ul);
  }
}