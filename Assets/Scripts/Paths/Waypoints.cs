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

  int? PreviousIndexWithUniquePosition(int n) {
    var p0 = Points[n].transform.position;
    var previousIndex = n;
    for (int i = 0; i < Points.Length; i++) {
      previousIndex = previousIndex <= 0 ? Points.Length-1 : previousIndex-1;
      var pprevious = Points[previousIndex].transform.position;
      if (p0 != pprevious) {
        return previousIndex;
      }
    }
    return null;
  }

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
        var iPrevious = PreviousIndexWithUniquePosition(i);
        var iNext = NextIndexWithUniquePosition(i);
        var delta = p1-p0;
        var f = Mathf.InverseLerp(d0,d1,interpolant);
        var position = p0+f*delta;
        if (f <= TurnFraction) {
          if (iPrevious.HasValue) {
            var pp = Points[iPrevious.Value].transform.position;
            var f0 = (p1-pp).XZ().normalized;
            var r0 = Quaternion.LookRotation(f0,Vector3.up);
            return new PathData(position,r0);
          } else {
            return new PathData(position,Quaternion.identity);
          }
        } else {
          if (iPrevious.HasValue) {
            if (iNext.HasValue) {
              var pp = Points[iPrevious.Value].transform.position;
              var pn = Points[iNext.Value].transform.position;
              var fp = (p1-pp).XZ().normalized;
              var fn = (pn-p1).XZ().normalized;
              var rp = Quaternion.LookRotation(fp,Vector3.up);
              var rn = Quaternion.LookRotation(fn,Vector3.up);
              var fraction = Mathf.InverseLerp(TurnFraction,1,f);
              var rotation = Quaternion.Slerp(rp,rn,fraction);
              return new PathData(position,rotation);
            } else {
              var pp = Points[iPrevious.Value].transform.position;
              var f0 = (p1-pp).XZ().normalized;
              var r0 = Quaternion.LookRotation(f0,Vector3.up);
              return new PathData(position,r0);
            }
          } else {
            return new PathData(position,Quaternion.identity);
          }
        }
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