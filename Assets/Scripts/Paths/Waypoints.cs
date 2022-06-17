using UnityEngine;

public class Waypoints : Path {
  [SerializeField] 
  Color Color;
  [SerializeField]
  [Range(0,1)]
  float TurnFraction;
  Waypoint[] Points;
  float TotalDistance;
  float[] Distances;
  float[] NormalizedDistances;

  int? NextIndexWithUniquePosition(int n) {
    var p0 = Points[n].transform.position;
    var nextIndex = n;
    for (int i = 0; i < Points.Length; i++) {
      nextIndex = nextIndex+1 >= Points.Length ? 0 : nextIndex+1;
      var pnext = Points[nextIndex].transform.position;
      if (p0 != pnext) {
        return nextIndex;
      }
    }
    return null;
  }

  public static PathData SegmentToWorldSpace(float interpolant, Vector3 p0, Vector3 p1, Vector3? p2, float d0, float d1, float turnFraction) {
    var delta = p1-p0;
    var f = Mathf.InverseLerp(d0, d1, interpolant);
    var position = p0+f*delta;
    if (f <= turnFraction || !p2.HasValue) {
      var r0 = Quaternion.LookRotation((p1-p0).XZ().normalized, Vector3.up);
      return new PathData(position, r0);
    } else {
      var r0 = Quaternion.LookRotation((p1-p0).XZ().normalized, Vector3.up);
      var r1 = Quaternion.LookRotation((p2.Value-p1).XZ().normalized, Vector3.up);
      var fraction = Mathf.InverseLerp(turnFraction, 1, f);
      var rotation = Quaternion.Slerp(r0, r1, fraction);
      return new PathData(position, rotation);
    }
  }

  public override PathData ToWorldSpace(float interpolant) {
    for (int i = 1; i < NormalizedDistances.Length; i++) {
      var d0 = NormalizedDistances[i-1];
      var d1 = NormalizedDistances[i];
      var onSegment = interpolant >= d0 && interpolant <= d1;
      if (onSegment) {
        var p0 = Points[i-1].transform.position;
        var p1 = Points[i].transform.position;
        var iNext = NextIndexWithUniquePosition(i);
        Vector3? p2 = iNext.HasValue ? Points[iNext.Value].transform.position : null;
        return SegmentToWorldSpace(interpolant, p0, p1, p2, d0, d1, TurnFraction);
      }
    }
    return new PathData(Points[0].transform.position,Points[0].transform.rotation);
  }

  void UpdateDistances() {
    Distances[0] = 0;
    for (int i = 1; i < Points.Length; i++) {
      var start = Points[i-1].transform.position;
      var end = Points[i].transform.position;
      Distances[i] = Vector3.Distance(start,end);
    }
  }

  void UpdateTotalDistance() {
    TotalDistance = 0;
    foreach (var distance in Distances) {
      TotalDistance += distance;
    }
  }

  void UpdateNormalizedDistances() {
    NormalizedDistances[0] = 0;
    for (int i = 1; i < NormalizedDistances.Length; i++) {
      NormalizedDistances[i] = NormalizedDistances[i-1]+Distances[i]/TotalDistance;
    }
  }

  void Awake() {
    Points = GetComponentsInChildren<Waypoint>(false);
    Distances = new float[Points.Length];
    NormalizedDistances = new float[Points.Length];
    UpdateDistances();
    UpdateTotalDistance();
    UpdateNormalizedDistances();
  }

  void FixedUpdate() {
    UpdateDistances();
    UpdateTotalDistance();
    UpdateNormalizedDistances();
  }
    
  void OnDrawGizmos() {
    Points = GetComponentsInChildren<Waypoint>(false);
    Distances = new float[Points.Length];
    NormalizedDistances = new float[Points.Length];
    UpdateDistances();
    UpdateTotalDistance();
    UpdateNormalizedDistances();
    Gizmos.color = Color;
    for (int i = 0; i < Points.Length-1; i++) {
      var start = Points[i].transform.position;
      var end = Points[i+1].transform.position;
      Gizmos.DrawLine(start,end);
    }
  }
}