using UnityEngine;

public class Conveyor : MonoBehaviour {
  [SerializeField]
  int FramesPerCycle = 1000;  
  [SerializeField]
  Path Path;

  Bucket[] Buckets;
  int FramesRemaining;

  void FixedUpdate() {
    if (FramesRemaining > 0) {
      FramesRemaining = FramesRemaining-1;
    } else {
      FramesRemaining = FramesPerCycle;
    }
    var interpolant = 1f-(float)FramesRemaining/(float)FramesPerCycle;
    foreach (var bucket in Buckets) {
      var distance = interpolant+bucket.Distance;
      var pathdata = Path.ToWorldSpace(distance%1f);
      bucket.transform.SetPositionAndRotation(pathdata.Position,pathdata.Rotation);
    }
  }

  void OnDrawGizmos() {
    Buckets = GetComponentsInChildren<Bucket>(false);
    if (!Application.isPlaying) {
      foreach (var bucket in Buckets) {
        var pathData = Path.ToWorldSpace(bucket.Distance);
        bucket.transform.position = pathData.Position;
        Gizmos.DrawWireSphere(pathData.Position,.25f);
      }
    }
  }
}