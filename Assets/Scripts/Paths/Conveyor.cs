using System.Collections.Generic;
using UnityEngine;

public class Conveyor : MonoBehaviour {
  public Timeval CycleTime = Timeval.FromMillis(3000);
  public Path Path;
  public List<Bucket> Buckets;

  BucketAction BucketAction;
  int FramesRemaining;

  void Awake() {
    BucketAction = GetComponent<BucketAction>();
  }

  void FixedUpdate() {
    var currentFrames = FramesRemaining;
    var nextFrames = FramesRemaining > 0 ? FramesRemaining-1 : CycleTime.Frames;
    var currentFraction = 1f-(float)currentFrames/(float)CycleTime.Frames;
    var nextFraction = 1f-(float)nextFrames/(float)CycleTime.Frames;
    foreach (var bucket in Buckets) {
      var currentDistance = (currentFraction+bucket.Distance)%1f;
      var nextDistance = (nextFraction+bucket.Distance)%1f;
      var pathdata = Path.ToWorldSpace(currentDistance);
      if (nextDistance < currentDistance) {
        BucketAction?.Invoke(bucket);
      }
      bucket.transform.SetPositionAndRotation(pathdata.Position, pathdata.Rotation);
    }
    FramesRemaining = nextFrames;
  }

  void OnDrawGizmos() {
    if (!Application.isPlaying) {
      foreach (var bucket in Buckets) {
        var pathData = Path.ToWorldSpace(bucket.Distance);
        bucket.transform.position = pathData.Position;
        Gizmos.DrawWireSphere(pathData.Position, .25f);
      }
    }
  }
}