using UnityEngine;

public class Conveyor : MonoBehaviour {
  public int FramesPerCycle = 1000;  

  Waypoint[] Waypoints;
  Bucket[] Buckets;
  float TotalDistance;
  float[] Distances;
  float[] NormalizedDistances;
  int FramesRemaining;

  Vector3 ToWorldSpace(float distance) {
    for (int i = 1; i < NormalizedDistances.Length; i++) {
      var d0 = NormalizedDistances[i-1];
      var d1 = NormalizedDistances[i];
      var onSegment = distance >= d0 && distance <= d1;
      if (onSegment) {
        var p0 = Waypoints[i-1].transform.position;
        var p1 = Waypoints[i].transform.position;
        var delta = p1-p0;
        var interpolant = Mathf.InverseLerp(d0,d1,distance);
        return p0+interpolant*delta;
      }
    }
    return Vector3.zero;
  }

  void UpdateDistances() {
    Distances[0] = 0;
    for (int i = 1; i < Waypoints.Length; i++) {
      var start = Waypoints[i-1].transform.position;
      var end = Waypoints[i].transform.position;
      Distances[i] = Vector3.Distance(start,end);
    }
  }

  void UpdateTotalDistance() {
    TotalDistance=0;
    foreach (var distance in Distances) {
      TotalDistance+=distance;
    }
  }

  void UpdateNormalizedDistances() {
    NormalizedDistances[0] = 0;
    for (int i = 1; i < NormalizedDistances.Length; i++) {
      NormalizedDistances[i] = NormalizedDistances[i-1]+Distances[i]/TotalDistance;
    }
  }

  void Awake() {
    FramesRemaining = FramesPerCycle;
    Waypoints = GetComponentsInChildren<Waypoint>(false);
    Buckets = GetComponentsInChildren<Bucket>(false);
    Distances = new float[Waypoints.Length];
    NormalizedDistances = new float[Waypoints.Length];
    UpdateDistances();
    UpdateTotalDistance();
    UpdateNormalizedDistances();
  }

  void FixedUpdate() {
    if (FramesRemaining > 0) {
      FramesRemaining = FramesRemaining-1;
    } else {
      FramesRemaining = FramesPerCycle;
    }
    var interpolant = (float)FramesRemaining/(float)FramesPerCycle;
    foreach (var bucket in Buckets) {
      var distance = interpolant+bucket.Distance;
      bucket.transform.position = ToWorldSpace(distance%1f);
    }
  }

  void OnDrawGizmos() {
    Waypoints = GetComponentsInChildren<Waypoint>(false);
    Buckets = GetComponentsInChildren<Bucket>(false);
    Distances = new float[Waypoints.Length];
    NormalizedDistances = new float[Waypoints.Length];
    UpdateDistances();
    UpdateTotalDistance();
    UpdateNormalizedDistances();
    for (int i = 0; i < Waypoints.Length-1; i++) {
      var start = Waypoints[i].transform.position;
      var end = Waypoints[i+1].transform.position;
      Gizmos.DrawLine(start,end);
    }
    if (!Application.isPlaying) {
      foreach (var bucket in GetComponentsInChildren<Bucket>(false)) {
        var position = ToWorldSpace(bucket.Distance);
        bucket.transform.position = position;
        Gizmos.DrawWireSphere(position,.25f);
      }
    }
  }
}