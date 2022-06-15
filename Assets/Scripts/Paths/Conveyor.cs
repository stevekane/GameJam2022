using UnityEngine;
using UnityEngine.Events;

public class Conveyor : MonoBehaviour {
  [SerializeField]
  int FramesPerCycle = 1000;  
  [SerializeField]
  Path Path;
  [SerializeField]
  BucketAction BucketAction;

  Bucket[] Buckets;
  int FramesRemaining;

  void FixedUpdate() {
    var currentFrames = FramesRemaining;
    var nextFrames = FramesRemaining > 0 ? FramesRemaining-1 : FramesPerCycle;
    var currentFraction = 1f-(float)currentFrames/(float)FramesPerCycle;
    var nextFraction = 1f-(float)nextFrames/(float)FramesPerCycle;
    foreach (var bucket in Buckets) {
      var currentDistance = (currentFraction+bucket.Distance)%1f;
      var nextDistance = (nextFraction+bucket.Distance)%1f;
      var pathdata = Path.ToWorldSpace(currentDistance);
      if (nextDistance < currentDistance) {
        BucketAction?.Invoke(bucket);
      }
      bucket.transform.SetPositionAndRotation(pathdata.Position,pathdata.Rotation);
    }
    FramesRemaining = nextFrames;
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