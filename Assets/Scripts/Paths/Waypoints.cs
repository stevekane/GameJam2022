using UnityEngine;

public class Waypoints : Path {
  [SerializeField] 
  Color Color;
  Waypoint[] Points;
  float TotalDistance;
  float[] Distances;
  float[] NormalizedDistances;

  public override PathData ToWorldSpace(float interpolant) {
    for (int i = 1; i < NormalizedDistances.Length; i++) {
      var d0 = NormalizedDistances[i-1];
      var d1 = NormalizedDistances[i];
      var onSegment = interpolant >= d0 && interpolant <= d1;
      if (onSegment) {
        var t0 = Points[i-1].transform;
        var t1 = Points[i].transform;
        var p0 = t0.position;
        var p1 = t1.position;
        var r0 = t0.rotation;
        var r1 = t1.rotation;
        var delta = p1-p0;
        var f = Mathf.InverseLerp(d0,d1,interpolant);
        var position = p0+f*delta;
        var rotation = Quaternion.Slerp(r0,r1,f);
        return new PathData(position,rotation);
      }
    }
    return new PathData(Vector3.zero,Quaternion.identity);
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