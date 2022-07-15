using UnityEngine;

[ExecuteInEditMode]
public class Wire : MonoBehaviour {
  public Waypoints Waypoints;
  public LineRenderer LineRenderer;

  public void OnContactEnd(WireEndpoint end, Vapor vapor) {
    vapor.RideWire(this);
  }

  void Update() {
    var points = Waypoints.Points;
    LineRenderer.positionCount = points.Length;
    for (var i = 0; i < points.Length; i++) {
      LineRenderer.SetPosition(i, points[i].transform.position);
    }
  }
}