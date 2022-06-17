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
    var top = Width/pathLength;
    var right = top+Height/pathLength;
    var bottom = right+Width/pathLength;
    var left = bottom+Height/pathLength;
    var ul = transform.position-transform.right*Width/2+transform.forward*Height/2;
    var ur = transform.position+transform.right*Width/2+transform.forward*Height/2;
    var lr = transform.position+transform.right*Width/2-transform.forward*Height/2;
    var ll = transform.position-transform.right*Width/2-transform.forward*Height/2;
    interpolant = interpolant%1f;
    if (interpolant < top) {
      var i = Mathf.InverseLerp(0,top,interpolant);
      return new PathData(ul+transform.right*i*Width, Quaternion.LookRotation(transform.right));
    } else if (interpolant < right) {
      var i = Mathf.InverseLerp(top,right,interpolant);
      return new PathData(ur-transform.forward*i*Height, Quaternion.LookRotation(-transform.forward));
    } else if (interpolant < bottom) {
      var i = Mathf.InverseLerp(right,bottom,interpolant);
      return new PathData(lr-transform.right*i*Width, Quaternion.LookRotation(-transform.right));
    } else {
      var i = Mathf.InverseLerp(bottom,left,interpolant);
      return new PathData(ll+transform.forward*i*Height, Quaternion.LookRotation(transform.forward));
    }
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